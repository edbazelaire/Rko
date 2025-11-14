using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Data.DataStructures.SpellSubStructures.Spawns;
using Enums;
using Game.Loaders;
using MyBox;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    

    [CreateAssetMenu(fileName = "Spawner", menuName = "Game/Spells/Spawner")]
    public class SpawnerData : SpellData
    {
        #region Members

        public override ESpellType SpellType => ESpellType.Spawner;

        // ===========================================================================
        // Serialized Data
        [Header("SpawnerData")]
        [SerializeField] protected bool                 m_IsUnique;
        [SerializeField] protected bool                 m_DestroySpawnsOnEnd;
        [SerializeField] protected SMultiSpellSpawn     m_SpawnTarget;

        // Spawn Management
        public bool IsWaveSpawn = false;
        // -- Simple Spawn
        [SerializeField, ConditionalField("IsWaveSpawn", true)] 
        protected int m_NSpawns = -1;
        [SerializeField, ConditionalField("IsWaveSpawn", true)]
        protected List<SSpawnElement> m_SpawnElements;
        // -- Waves
        [SerializeField, ConditionalField("IsWaveSpawn")] 
        protected SWaveGroup           m_Waves;
        [SerializeField, ConditionalField("IsWaveSpawn")] 
        protected List<GameObject>     m_WavesGraphics;

        // ===========================================================================
        // Public Accessors
        public int NWaves                               => m_Waves.WaveSpawns.Count;
        public bool IsUnique                            => m_IsUnique;
        public bool DestroySpawnsOnEnd                  => m_DestroySpawnsOnEnd;
        public SMultiSpellSpawn SpawnTarget             => m_SpawnTarget;
        public SWaveGroup Waves                         => m_Waves;
        public int NSpawns                              => (int)GetScaledValue(ESpellProperty.NSpawns, m_NSpawns);
        public List<SSpawnElement> SpawnElements        => IsWaveSpawn ? new() : m_SpawnElements;

        #endregion


        #region Target

        public override void CalculateTarget(ref Vector3 target, ulong clientId, ulong? targetId)
        {
            target.y = 0;
            base.CalculateTarget(ref target, clientId, targetId);
        }

        #endregion


        #region Waves Management

        public void SetSpawnElements(List<SSpawnElement> spawnElements)
        {
            m_SpawnElements = spawnElements;
        }

        public void SetWaves(SWaveGroup waves)
        {
            m_Waves = waves;
        }

        public GameObject GetWavesGraphicsAtIndex(int index)
        {
            if (index < 0 || m_WavesGraphics.Count <= index)
                return null;

            return m_WavesGraphics[index];
        }

        #endregion


        #region Infos

        public SpawnerData Clone(int level)
        {
            return base.Clone(level) as SpawnerData;
        }

        public override Dictionary<string, object> GetInfo()
        {
            var infos = base.GetInfo();

            if (! m_SpawnElements.IsNullOrEmpty())
            {
                infos["Spawns"] = GetSpawnsCharacterData();
            }

            if (NSpawns > 1)
            {
                infos["Invocations"] = NSpawns;
            }

            return infos;
        }

        public List<CharacterData> GetSpawnsCharacterData()
        {
            var spawns = new List<CharacterData>();
            foreach (var spawn in m_SpawnElements)
            {
                var charData = CharacterLoader.GetCharacterData(spawn.CharacterName);
                charData.SetLevel(m_Level + spawn.BonusLevel);
                spawns.Add(charData);
            }

            return spawns;
        }

        public override string GetDescription()
        {
            return TextHandler.ReplaceSpawnTokens(base.GetDescription(), GetSpawnsCharacterData());
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