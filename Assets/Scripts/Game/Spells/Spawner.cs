using Data;
using Enums;
using Game.Loaders;
using Managers;
using System;
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

        protected int   m_NSpawnsCounter;
        protected bool  m_HasSpawnLeft;
        protected float m_DurationTimer;
        protected List<Controller> m_Spawns = new ();

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="damage"></param>
        /// <param name="duration"></param>
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            base.Initialize(clientId, target, spellName, level, parent);

            if (!IsServer)
                return;

            m_NSpawnsCounter    = 0;
            m_HasSpawnLeft      = true;

            // initialize spawners delays
            InitSpawns();

            // setup radius and timer
            m_DurationTimer     = m_SpellData.Duration;
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        protected override void End()
        {
            StopAllCoroutines();
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

            // if inifite zone, do nothing
            if (m_SpellData.Duration <= -1f)
                return;

            m_DurationTimer -= Time.deltaTime;
            if (m_DurationTimer <= 0f && ! m_IsOver)
            {
                End();
                return;
            }
        }

        #endregion


        #region Spawn

        void InitSpawns()
        {
            // check if all spawnElements has been spawned
            foreach (var spawnElement in m_SpellData.SpawnElements)
            {
                StartCoroutine(spawnElement.StartDelayTimer());
            }
        }

        void CheckSpawns()
        {
            if (!m_HasSpawnLeft)
                return;

            // check if all spawnElements has been spawned
            bool hasAnySpawnLeft = false;

            var currentProba = 0f;
            float random = UnityEngine.Random.Range(0f, 1f);
            for (int i = 0; i < m_SpellData.SpawnElements.Count; i++)
            {
                var spawnElement = m_SpellData.SpawnElements[i];
                currentProba += spawnElement.SpawnProbability;

                if (spawnElement.NSpawnCounter >= spawnElement.MaxSpawns && spawnElement.MaxSpawns > 0)
                    continue;

                hasAnySpawnLeft = true;

                if (! spawnElement.CanSpawn)
                    continue;

                if (currentProba < random)
                    continue;
                
                Spawn(ref spawnElement);
                m_SpellData.SpawnElements[i] = spawnElement;
                break;
            }

            if (!hasAnySpawnLeft || (m_SpellData.NSpawns > 0 && m_NSpawnsCounter >= m_SpellData.NSpawns))
                m_HasSpawnLeft = false;
        }

        void Spawn(ref SSpawnElement spawnElement)
        {
            ErrorHandler.Log("Spawning [" + spawnElement.NSpawnCounter + "] : " + spawnElement.CharacterName, ELogTag.Spawns);

            // create an AI prefab and spawn it
            var spawnPrefab = Instantiate(
                CharacterLoader.GetPrefab(spawnElement.CharacterName, false), 
                CalculateSpawnPosition(spawnElement.NSpawnCounter, spawnElement.MaxSpawns), 
                Quaternion.Euler(0f, 0f, 0f)
            );

            spawnPrefab.GetComponent<NetworkObject>().Spawn(true);
        
            // add player to list of player controllers
            Controller spawnController = Finder.FindComponent<Controller>(spawnPrefab);

            // initialize player data
            spawnController.InitializeSpawn(
                playerData: CreatePlayerData(spawnElement),
                team:       m_Controller.Team
            );

            // setup Kill coroutine if has duration
            if (spawnElement.Duration > 0)
                spawnController.StartCoroutine(KillSpawn(spawnController, spawnElement.Duration));

            // update variables
            spawnElement.NSpawnCounter++;
            m_NSpawnsCounter++;
            m_Spawns.Add(spawnController);

            // link event that will remove spawn from list on external destruction
            spawnController.OnDestroyedEvent += () => { OnSpawnDestroyed(spawnController); };

            // set next timer before spawning
            StartCoroutine(spawnElement.StartSpawnTimer());
        }

        SPlayerData CreatePlayerData(SSpawnElement spawnElement)
        {
            return new SPlayerData(
                playerName:     "",
                characterLevel: Math.Max(1, m_SpellData.Level + spawnElement.BonusLevel),
                character:      spawnElement.CharacterName,
                isPlayer:       false
            );
        }

        IEnumerator KillSpawn(Controller spawnController, float duration)
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

        protected virtual Vector3 CalculateSpawnPosition(int index, int maxSpawns)
        {
            return m_SpellData.SpawnTarget.RecalculateTarget(transform.position, index, m_Controller.Team, maxSpawns);
        }

        #endregion


        #region Listeners

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            int nSpawns = m_Spawns.Count;
            for (int i = 0; i < nSpawns; i++)
            {
                // in case a spawns dies during the process
                if (i > m_Spawns.Count)
                    return;

                m_Spawns[i].OnDestroyedEvent -= () => { OnSpawnDestroyed(m_Spawns[i]); };
            }
        }

        void OnSpawnDestroyed(Controller spawnController)
        {
            m_Spawns.Remove(spawnController);

            // if no more spawns and can not spawn any more -> end
            if (m_Spawns.Count == 0 && ! m_HasSpawnLeft)
                End();
        }

        #endregion
    }
}