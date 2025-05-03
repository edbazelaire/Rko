using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Enums;
using Game.Loaders;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class SSpawnElement
    {
        // ===========================================================================
        // Serialized Data
        [Tooltip("Name of the character to spawn")]
        public string       CharacterName;                   
        [SerializeField, Tooltip("max number of spawn for that element (-1 to infinite)")]
        protected int       m_MaxSpawns           = -1;       
        [SerializeField, Tooltip("probability that this spawn element is selected")]
        protected float     m_SpawnProbability    = 1f;      
        [SerializeField, Tooltip("bonus levels of the spawn")]
        public int          m_BonusLevel          = 0;       
        [SerializeField, Tooltip("interval between each spawns")]
        public float        m_SpawnInterval       = 0f;     
        [SerializeField, Tooltip("delay before starting")]
        public float        m_Delay               = 0f;    
        [SerializeField, Tooltip("duration of the spawn (-1 to infinite)")]
        public float        m_Duration            = -1f;      

        // ===========================================================================
        // Private Data
        protected bool  m_CanSpawn;
        protected int   m_NSpawnCounter;

        // ===========================================================================
        // Public Accessors
        public int MaxSpawns            => m_MaxSpawns;
        public float SpawnProbability   => m_SpawnProbability;
        public int BonusLevel           => m_BonusLevel;
        public float SpawnInterval      => m_SpawnInterval;
        public float Delay              => m_Delay;
        public float Duration           => m_Duration;
        public bool CanSpawn            => m_CanSpawn;
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
        [SerializeField] protected int                  m_NSpawns = -1;
        [SerializeField] protected List<SSpawnElement>  m_SpawnElements;
        
        public int NSpawns                              => m_NSpawns;
        public bool IsUnique                            => m_IsUnique;
        public bool DestroySpawnsOnEnd                  => m_DestroySpawnsOnEnd;
        public SMultiSpellTarget SpawnTarget            => m_SpawnTarget;
        public List<SSpawnElement> SpawnElements        => m_SpawnElements;

        #endregion


        #region Target

        public override void CalculateTarget(ref Vector3 target, ulong clientId, ulong? targetId)
        {
            target.y = 0;
            base.CalculateTarget(ref target, clientId, targetId);
        }

        #endregion


        #region Infos

        public override Dictionary<string, object> GetInfo()
        {
            var infos = base.GetInfo();

            if (IsUniqueSpawn)
            {
                var charData = CharacterLoader.GetCharacterData(m_SpawnElements[0].CharacterName);
                charData.SetLevel(m_Level);
                infos["Hp"] = charData.MaxHealth;
            }

            return infos;
        }

        public override string GetTypeInfo()
        {
            if (IsStruct)
                return "Structure";

            return base.GetTypeInfo();
        }

        public bool IsStruct => 
            IsUniqueSpawn
            && CharacterLoader.GetCharacterData(m_SpawnElements[0].CharacterName).IsStructure;

        public bool IsUniqueSpawn => 
            m_SpawnElements.Count == 1
            && m_SpawnElements[0].MaxSpawns == 1;

        #endregion
    }
}