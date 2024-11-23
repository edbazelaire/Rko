using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Enums;
using Game.Loaders;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class SSpawnElement
    {
        public string   CharacterName;                  // name of the character to spawn
        public int      NSpawn              = 1;        // number of spawn (-1 to infinite)
        public float    SpawnInterval       = 0f;       // interval between each spawns
        public float    Delay               = 0f;       // delay before starting
        public float    Duration            = -1f;      // duration of the spawn (-1 to infinite)

        protected bool m_CanSpawn;
        protected int m_NSpawnCounter;
        public bool CanSpawn => m_CanSpawn;
        public int NSpawnCounter { get { return m_NSpawnCounter; } set { m_NSpawnCounter = value; } }


        public IEnumerator StartDelayTimer()
        {
            m_CanSpawn = false;
            m_NSpawnCounter = 0;
            yield return new WaitForSeconds(Delay);
            m_CanSpawn = true;
        }

        public IEnumerator StartSpawnTimer()
        {
            ErrorHandler.Log("Start Spawn timer : " + CharacterName, ELogTag.Spawns);

            m_CanSpawn = false;
            yield return new WaitForSeconds(SpawnInterval);
            m_CanSpawn = true;

            ErrorHandler.Log("Ended Spawn timer : " + CharacterName, ELogTag.Spawns);
        }
    }

    [CreateAssetMenu(fileName = "Spawner", menuName = "Game/Spells/Spawner")]
    public class SpawnerData : SpellData
    {
        #region Members

        public override ESpellType SpellType => ESpellType.Spawner;

        [Header("SpawnerData")]
        [SerializeField] protected bool                 m_IsUnique;
        [SerializeField] protected bool                 m_DestroySpawnsOnEnd;
        [SerializeField] protected SMultiSpellTarget    m_SpawnTarget;
        [SerializeField] protected List<SSpawnElement>  m_SpawnElements;

        public bool IsUnique                            => m_IsUnique;
        public bool DestroySpawnsOnEnd                  => m_DestroySpawnsOnEnd;
        public SMultiSpellTarget SpawnTarget            => m_SpawnTarget;
        public List<SSpawnElement> SpawnElements        => m_SpawnElements;

        #endregion


        public override void CalculateTarget(ref Vector3 target, ulong clientId)
        {
            target.y = 0;
            base.CalculateTarget(ref target, clientId);
        }


        #region Infos

        public override Dictionary<string, object> GetInfos()
        {
            var infos = base.GetInfos();

            if (IsStruct)
            {
                var charData = CharacterLoader.GetCharacterData(m_SpawnElements[0].CharacterName);
                infos["Hp"] = charData.MaxHealth;
            }

            return infos;
        }

        public override string GetTypeInfo()
        {
            if (IsStruct)
                return "Structure";

            return base.GetTargetTypeInfo();
        }

        public bool IsStruct => 
            m_SpawnElements.Count == 1
            && m_SpawnElements[0].NSpawn == 1
            && CharacterLoader.GetCharacterData(m_SpawnElements[0].CharacterName).IsStructure;

        #endregion
    }
}