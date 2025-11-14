using Enums;
using Game.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures.Spawns
{
    [Serializable]
    public struct SWaveGroup
    {
        public List<SWaveSpawn> WaveSpawns;

        public SWaveSpawn GetWaveAtIndex(int index)
        {
            if (index < 0 || index >= WaveSpawns.Count())
            {
                ErrorHandler.Warning($"Bad wave index provided ({index}) / {WaveSpawns.Count()}");
                return null;
            }

            return WaveSpawns[index];
        }

        /// <summary>
        /// Help method to list all unique spawns
        /// </summary>
        /// <returns></returns>
        public List<ESpawn> GetListOfSpawns()
        {
            List<ESpawn> list = new();
            foreach (var wave in WaveSpawns)
            {
                foreach (var spawnGroup in wave.SpawnGroups)
                {
                    foreach (var spawnElement in spawnGroup.SpawnElements)
                    {
                        if (! Enum.TryParse(spawnElement.CharacterName, out ESpawn spawn))
                        {
                            ErrorHandler.Warning($"Unable to convert {spawnElement.CharacterName} as Spawn");
                            continue;
                        }

                        if (list.Contains(spawn))
                            continue;

                        list.Add(spawn);
                    }
                }
            }

            return list;
        }
    }

    [Serializable]
    public class SWaveSpawn
    {
        #region Members

        // ===========================================================================
        // Serialized Data
        [SerializeField, Tooltip("Time before next wave is spawning (-1 to infinite)")]
        public float m_WaveDuration = -1f;
        [SerializeField, Tooltip("Max number of spawn death before going to the next wave")]
        public int m_MaxDeath = -1;
        [SerializeField, Tooltip("Delay before starting")]
        public float m_Delay = 0f;
        [SerializeField, Tooltip("List of elements to spawn")]
        public List<SSpawnGroup> m_SpawnGroups;

        // ===========================================================================
        // Private Data
        protected bool m_IsOver;                        // wave is over, it needs to go to the next one
        protected bool m_CanSpawn;                      // wave can spawn elements
        protected int m_DeathCounter;                   // number of spawns death in this wave
        List<Controller> m_Spawns = new();              // current spawns in the wave (carries the spawns of previous waves)
        protected bool m_IsLastWave = false;            // is this the last wave ? (requires to kill all current spawns to be cleared)
        Dictionary<string, int> m_KillCounter = new();  // countdown of the "main spawns" of the wave. Once counter reaches 0, start next wave.

        // ===========================================================================
        // Public Accessors
        public bool                 IsOver          => m_IsOver;
        public bool                 HasSpawnLeft    => m_SpawnGroups.Any(t => t.HasSpawnLeft);
        public bool                 CanSpawn        => ! m_IsOver && m_CanSpawn && HasSpawnLeft && m_SpawnGroups.Any(t => t.CanSpawn);
        public float                Delay           => m_Delay;
        public List<SSpawnGroup>    SpawnGroups     => m_SpawnGroups;
        public List<Controller>     Spawns          => m_Spawns;
        public Dictionary<string, int> KillCounter  => m_KillCounter;


        #endregion


        #region Timers

        public IEnumerator StartDelayTimer(List<Controller> previousSpawns, bool isLastWave)
        {
            // reset data
            m_IsLastWave = isLastWave;
            m_KillCounter = new();
            m_IsOver = false;
            m_DeathCounter = 0;

            // add spawns from last wave
            AddPreviousSpawns(previousSpawns);

            // wait for delay
            m_CanSpawn = false;
            yield return new WaitForSeconds(Delay);
            m_CanSpawn = true;

            ErrorHandler.Log("-- Wave is starting", ELogTag.SpawnWaves);

            // call delay timer of the sub-spawn groups
            foreach (var spawnGroup in m_SpawnGroups)
            {
                spawnGroup.Start();
                m_KillCounter = m_KillCounter.Concat(spawnGroup.KillCounter)
                  .GroupBy(kv => kv.Key)
                  .ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value));
            }
        }

        public IEnumerator StartWaveDurationTimer()
        {
            if (m_WaveDuration <= 0)
                yield break;

            yield return new WaitForSeconds(m_WaveDuration);

            if (m_IsOver)
                yield break;

            m_IsOver = true;
        }

        #endregion


        #region End

        public void End()
        {
            // already over
            if (m_IsOver)
                return;

            m_IsOver = true;
            UnRegisterListeners();

            ErrorHandler.Log("-- Wave is ending", ELogTag.SpawnWaves);
        }

        #endregion


        #region Spawn

        void AddPreviousSpawns(List<Controller> previousSpawns)
        {
            m_Spawns = previousSpawns; 
            
            foreach (var spawn in previousSpawns)
            {
                spawn.Life.OnDeathEvent += () => OnSpawnDeath(spawn);
            }
        }

        public void Spawn(Spawner spell)
        {
            CheckIsOver();

            if (m_IsOver)
                return;

            if (! CanSpawn)
                return;

            // call Spawn on all SpawnGroups
            List<Controller> spawnControllers = new();
            Dictionary<string, int> killCounter = new();
            foreach (var spawnGroup in m_SpawnGroups)
            {
                spawnControllers.AddRange(spawnGroup.Spawn(spell));
            }

            // Attach "OnDeath" callback
            for (int i = 0; i < spawnControllers.Count; i++)
            {
                var spawnController = spawnControllers[i];
                spawnControllers[i].Life.OnDeathEvent += () => OnSpawnDeath(spawnController);
            }

            // add spawns to list of Wave's Spawns
            m_Spawns.AddRange(spawnControllers);

            return;
        }

        void CheckIsOver()
        {
            if (m_IsOver)
                return;

            // remove all "null" controllers if any
            m_Spawns.RemoveAll(x => x == null || x.IsDestroyed());

            if (m_Spawns.Count == 0 && !HasSpawnLeft)
                End();
        }

        #endregion


        #region Listeners

        public void UnRegisterListeners()
        {
            int nSpawns = m_Spawns.Count;
            for (int i = 0; i < nSpawns; i++)
            {
                // in case a spawns dies during the process
                if (i > m_Spawns.Count)
                    return;

                m_Spawns[i].Life.OnDeathEvent -= () => OnSpawnDeath(m_Spawns[i]);
            }
        }

        /// <summary>
        /// When a Spawn dies, recheck if Wave is over or not
        /// </summary>
        /// <param name="spawnController"></param>
        void OnSpawnDeath(Controller spawnController)
        {
            // already over - exit
            if (m_IsOver)
                return;

            // increase number of dead spawns
            m_DeathCounter++;

            // remove this Spawn from list of active Spawns
            m_Spawns.Remove(spawnController);

            if (m_IsLastWave)
            {
                ErrorHandler.Log("LAST WAVE - Spawn Counter : " + m_Spawns.Count, ELogTag.SpawnWaves);
                if (m_Spawns.Count == 0)
                    End();
            }
                
            // if max number of death is reached - Wave is over
            if (m_MaxDeath > 0 && m_DeathCounter > m_MaxDeath)
                End();

            else if (m_KillCounter.ContainsKey(spawnController.Character) && m_KillCounter[spawnController.Character] > 0)
            {
                m_KillCounter[spawnController.Character] -= 1;
                ErrorHandler.Log("Kill Counter : " + m_KillCounter.Values.Sum(), ELogTag.SpawnWaves);
                if (m_KillCounter.Values.Sum() <= 0 && !m_IsLastWave)
                    End();
            }

            // if can't spawn and has no Spawns left - Wave is over
            else if (m_Spawns.Count == 0 && !HasSpawnLeft)
                End();
        }

        #endregion
    }
}