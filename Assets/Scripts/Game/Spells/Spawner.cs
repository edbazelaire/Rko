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

        protected bool m_HasSpawnLeft;
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
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level)
        {
            base.Initialize(clientId, target, spellName, level);
            transform.position = new Vector3(target.x, m_SpellData.YPos, target.y);

            if (!IsServer)
                return;

            m_HasSpawnLeft = true;

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

            for (int i = 0; i < m_SpellData.SpawnElements.Count; i++)
            {
                var spawnElement = m_SpellData.SpawnElements[i];

                if (spawnElement.NSpawnCounter >= spawnElement.NSpawn && spawnElement.NSpawn > 0)
                    continue;

                hasAnySpawnLeft = true;

                if (! spawnElement.CanSpawn)
                    continue;
                
                Spawn(ref spawnElement);
                m_SpellData.SpawnElements[i] = spawnElement;
            }

            if (!hasAnySpawnLeft)
                m_HasSpawnLeft = false;
        }

        void Spawn(ref SSpawnElement spawnElement)
        {
            // create an AI prefab and spawn it
            var spawnPos = new Vector3(transform.position.x, m_SpellData.YPos, 0f);
            var spawnPrefab = Instantiate(CharacterLoader.GetCharacterData(spawnElement.CharacterName).IsStructure ? CharacterLoader.Instance.StructurePrefab : CharacterLoader.Instance.PlayerAIPrefab, spawnPos, Quaternion.Euler(0f, 0f, 0f));
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
            m_Spawns.Add(spawnController);

            // link event that will remove spawn from list on external destruction
            spawnController.OnDestroyedEvent += () => { OnSpawnDestroyed(spawnController); };

            // set next timer before spawning
            StartCoroutine(spawnElement.StartSpawnTimer());
        }

        SPlayerData CreatePlayerData(SSpawnElement spawnElement)
        {
            return new SPlayerData(
                playerName: "",
                characterLevel: m_SpellData.Level,
                character: spawnElement.CharacterName,
                isPlayer: false
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


        #region Listeners

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