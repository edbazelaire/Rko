using Assets.Scripts.Game;
using Assets.Scripts.Managers.Sound;
using Data;
using Data.DataStructures.SpellSubStructures.Spawns;
using Enums;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Spells
{
    public class Spawner : Spell
    {
        #region Members

        protected SpawnerData m_SpellData => (SpawnerData)m_BaseSpellData;
        protected Life m_Life;

        // ===========================================================================
        // Private Data
        int m_CurrentWaveIndex;

        // ===========================================================================
        // Dependent Members
        SWaveSpawn m_CurrentWave    => m_SpellData.Waves.GetWaveAtIndex(m_CurrentWaveIndex);
        SWaveSpawn m_PreviousWave   => m_CurrentWaveIndex > 0 ? m_SpellData.Waves.GetWaveAtIndex(m_CurrentWaveIndex - 1) : null;
        bool HasNextWave            => m_SpellData.NWaves > m_CurrentWaveIndex + 1;

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="target"></param>
        /// <param name="spellData"></param>
        public override void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            base.Initialize(clientId, target, spellData);

            if (!IsServer)
                return;

            if (m_SpellData.IsWaveSpawn)
            {
                StartWaveSpawns();
            } 
            // otherwise - spawn elements and end the spell
            else
            {
                StartInstantSpawn();
            }
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        protected override void End()
        {
            if (m_IsOver)
                return;

            StopAllCoroutines();
            if (m_CurrentWave != null)
                m_CurrentWave.End();

            base.End();
        }

        #endregion


        #region Update

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {
            if (m_IsOver)
                return;

            base.Update();

            if (!IsServer)
                return;

            CheckSpawns();
        }

        #endregion


        #region Instant Spawn

        void StartInstantSpawn()
        {
            if (m_SpellData.NSpawns > 0)
            {
                for (int i = 0; i < m_SpellData.NSpawns; i++)
                {
                    float proba = Random.Range(0f, 1f);
                    float currentProba = 0f;
                    var spawnElements = m_SpellData.SpawnElements.ShuffleClone();
                    for (int j = 0; j < spawnElements.Count; j++)
                    {
                        currentProba += spawnElements[j].SpawnProbability;

                        if (!spawnElements[j].HasSpawnLeft)
                            continue;

                        if (currentProba < proba)
                            continue;

                        spawnElements[j].Spawn(this);
                        break;
                    }
                }
            }
            else
            {
                foreach (var spawnElement in m_SpellData.SpawnElements)
                {
                    spawnElement.Spawn(this);
                }
            }


            End();
        }

        #endregion


        #region Wave Spawn

        void StartWaveSpawns()
        {
            // initialize spawners delays
            m_CurrentWaveIndex = 0;
            StartCurrentWave();

            // setup radius and timer
            m_DurationTimer = m_SpellData.Duration;
        }

        void StartCurrentWave()
        {
            ErrorHandler.Log($"STARTING WAVE [{m_CurrentWaveIndex+1}/{m_SpellData.NWaves}] ==========================================", ELogTag.SpawnWaves);
            if (m_CurrentWave == null)
            {
                StartNextWave();
                return;
            }

            if (GameManager.Instance.IsOfflineMode)
                SpawnWaveGraphics();
            else
                SpawnWaveGraphicsClientRPC();

            List<Controller> previousSpawns = m_PreviousWave != null ? m_PreviousWave.Spawns : new();
            StartCoroutine(m_CurrentWave.StartDelayTimer(previousSpawns, isLastWave: !HasNextWave));
        }

        [ClientRpc]
        void SpawnWaveGraphicsClientRPC()
        {
            SpawnWaveGraphics();
        }

        void SpawnWaveGraphics()
        {
            var graphics = m_SpellData.GetWavesGraphicsAtIndex(m_CurrentWaveIndex);
            if (graphics == null)
                return;

            // Initialize Graphics Container
            UIHelper.CleanContent(m_GraphicsContainer);

            m_Graphics = PoolManager.Pool(graphics, m_GraphicsContainer.transform, activate: false);
            m_Graphics.transform.localScale = Vector3.one;
            m_Graphics.transform.localPosition = Vector3.zero;
            m_Graphics.SetActive(true);

            var audioSource = Finder.FindComponent<AudioSource>(m_Graphics);
            if (audioSource != null)
                SoundFXManager.AdjustVolume(ref audioSource);
        }

        void CheckSpawns()
        {
            if (m_CurrentWave.IsOver)
            {
                StartNextWave();
                return;
            }

            m_CurrentWave.Spawn(this);
        }

        void StartNextWave()
        {
            if (!HasNextWave)
            {
                End();
                return;
            }

            m_CurrentWaveIndex++;
            StartCurrentWave();
        }

        public IEnumerator KillSpawn(Controller spawnController, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (spawnController == null || spawnController.gameObject.IsDestroyed())
                yield break;
            
            Destroy(spawnController.gameObject);
        }

        #endregion


        #region Target & Position

        protected override void SetTarget(Vector3 target)
        {
            transform.position = target;
            base.SetTarget(target);
        }

        public virtual Vector3 CalculateSpawnPosition(int index, int maxSpawns)
        {
            return m_SpellData.SpawnTarget.Recalculate(transform.position, index, m_Caster.Team, maxSpawns);
        }

        #endregion


        #region Listeners

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}