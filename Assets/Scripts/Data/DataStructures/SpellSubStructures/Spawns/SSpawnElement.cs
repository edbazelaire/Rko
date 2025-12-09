using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Data.Characters;
using Enums;
using Game;
using Game.Loaders;
using Game.Spells;
using Managers;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures.Spawns
{
    [Serializable]
    public class SSpawnGroup
    {
        public List<SSpawnElement> SpawnElements;

        public Dictionary<string, int> KillCounter;
        public bool HasSpawnLeft => SpawnElements.Any(t => t.HasSpawnLeft);
        public bool CanSpawn => HasSpawnLeft && SpawnElements.Any(t => t.CanSpawn);

        public void Start()
        {
            KillCounter = new();

            foreach (var spawnElement in SpawnElements)
            {
                GameManager.Instance.StartCoroutine(spawnElement.StartDelayTimer());

                // check if this is a burst of spawns
                if (spawnElement.SpawnInterval > 1 || spawnElement.MaxSpawns <= 0 || spawnElement.Delay > 0)
                    continue;

                // increase kill counter
                if (! KillCounter.ContainsKey(spawnElement.CharacterName))
                    KillCounter[spawnElement.CharacterName] = 0;
                KillCounter[spawnElement.CharacterName] += spawnElement.MaxSpawns;
            }
        }

        public List<Controller> Spawn(Spawner spell)
        {
            List<Controller> spawnControllers = new List<Controller>();

            // check if all spawnElements has been spawned
            var currentProba = 0f;
            float random = UnityEngine.Random.Range(0f, 1f);
            for (int i = 0; i < SpawnElements.Count; i++)
            {
                currentProba += SpawnElements[i].SpawnProbability;

                if (! SpawnElements[i].HasSpawnLeft)
                    continue;

                if (! SpawnElements[i].CanSpawn)
                    continue;

                if (currentProba < random)
                    continue;

                spawnControllers.AddRange(SpawnElements[i].Spawn(spell));
                break;
            }

            return spawnControllers;
        }
    }

    [Serializable]
    public class SSpawnElement
    {
        // ===========================================================================
        // Serialized Data
        [Tooltip("Name of the creature to spawn")]
        public string           CharacterName;
        [SerializeField, Tooltip("max number of spawn for that element (-1 to infinite)")]
        protected int           m_MaxSpawns         = -1;
        [SerializeField, Tooltip("Number of spawns at once")]
        protected int           m_NSpawns           = 1;
        [SerializeField, Tooltip("probability that this spawn element is selected")]
        protected float         m_SpawnProbability  = 1f;
        [SerializeField, Tooltip("bonus levels of the spawn")]
        public int              m_BonusLevel        = 0;
        [SerializeField, Tooltip("interval between each spawns")]
        public float            m_SpawnInterval     = 0f;
        [SerializeField, Tooltip("delay before starting")]
        public float            m_Delay             = 0f;
        [SerializeField, Tooltip("duration of the spawn (-1 to infinite)")]
        public float            m_Duration          = -1f;
        [SerializeField, Tooltip("Does the Spawn recalculate its own position ?")]
        public bool             m_RecalculatePosition = false;
        [SerializeField, ConditionalField("m_RecalculatePosition"), Tooltip("Position to Spawn")]
        public SMultiSpellSpawn m_SpawnPosition     = new();

        // ===========================================================================
        // Private Data
        protected bool m_Abort          = false;
        protected bool m_CanSpawn       = false;
        protected int m_NSpawnCounter   = 0;

        // ===========================================================================
        // Public Accessors
        public int      MaxSpawns => m_MaxSpawns;
        public float    SpawnProbability => m_SpawnProbability;
        public int      BonusLevel => m_BonusLevel;
        public float    SpawnInterval => m_SpawnInterval;
        public float    Delay => m_Delay;
        public float    Duration => m_Duration;
        public bool     HasSpawnLeft => ! m_Abort && (MaxSpawns <= 0 || NSpawnCounter < MaxSpawns);
        public bool     CanSpawn => m_CanSpawn;
        public int      NSpawnCounter { get { return m_NSpawnCounter; } set { m_NSpawnCounter = value; } }


        #region Timers

        public IEnumerator StartDelayTimer()
        {
            m_Abort         = false;
            m_CanSpawn      = false;
            m_NSpawnCounter = 0;
            yield return new WaitForSeconds(Delay);
            m_CanSpawn      = true;
        }

        public IEnumerator StartSpawnTimer()
        {
            ErrorHandler.Log(() => "Start Spawn timer : " + CharacterName, ELogTag.Spawns);

            m_CanSpawn = false;
            yield return new WaitForSeconds(SpawnInterval);
            m_CanSpawn = true;

            ErrorHandler.Log(() => "Ended Spawn timer : " + CharacterName, ELogTag.Spawns);
        }

        #endregion


        #region Spawn

        /// <summary>
        /// Spawn multiple controllers (as requested)
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public List<Controller> Spawn(Spawner spell) 
        {
            if (! HasSpawnLeft)
                return new();

            List<Controller> list = new();

            int nSpawns = m_NSpawns;
            if (m_MaxSpawns > 0)
                nSpawns = Math.Clamp(nSpawns, 1, m_MaxSpawns - m_NSpawnCounter);

            for (int i = 0; i < nSpawns; i++)
            {
                var spawnController = SpawnOne(spell);
                if (spawnController == null)
                {
                    m_Abort = true;
                    return new();
                }
                list.Add(spawnController);
            }

            // set next timer before spawning
            spell.StartCoroutine(StartSpawnTimer());

            return list;
        }

        /// <summary>
        /// Spawn one Controller
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public Controller SpawnOne(Spawner spell)
        {
            ErrorHandler.Log(() => "Spawning [" + NSpawnCounter + "] : " + CharacterName, ELogTag.Spawns);

            var offset = (CharacterLoader.GetCharacterData(CharacterName) is SpawnData data) ? data.GetSpawnOffset() : Vector3.zero;
            var position = m_RecalculatePosition ? 
                m_SpawnPosition.Recalculate(spell.transform.position, NSpawnCounter, spell.Caster.Team, MaxSpawns)
                : spell.CalculateSpawnPosition(NSpawnCounter, MaxSpawns) + offset;

            // create an AI prefab and spawn it
            var prefab = CharacterLoader.GetPrefab(CharacterName, false);
            if (prefab == null)
            {
                return null;
            }

            var spawnPrefab = GameObject.Instantiate(
                prefab,
                position,
                Quaternion.Euler(0f, 0f, 0f)
            );

            spawnPrefab.GetComponent<NetworkObject>().Spawn(true);

            // add player to list of player controllers
            Controller spawnController = Finder.FindComponent<Controller>(spawnPrefab);

            // initialize player data
            spawnController.InitializeSpawn(
                playerData: CreatePlayerData(spell.SpellData.Level),
                team:       spell.Caster.Team,
                spawnOwner: spell.Caster
            );

            // setup Kill coroutine if has duration
            if (Duration > 0)
                spawnController.StartCoroutine(spell.KillSpawn(spawnController, Duration));

            // update variables
            NSpawnCounter++;

            // return the controller of the Spawn
            return spawnController;
        }

        SPlayerData CreatePlayerData(int spellLevel)
        {
            return new SPlayerData(
                playerName:     "",
                characterLevel: Math.Max(1, spellLevel + BonusLevel),
                character:      CharacterName,
                isPlayer:       false,
                botData:        new SBotData()
            );
        }

        #endregion
    }
}