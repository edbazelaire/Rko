using Data.DataStructures.SpellSubStructures.Spawns;
using MyBox;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Data.GameManagement
{
    [CreateAssetMenu(fileName = "EthernalMenagerieData", menuName = "Game/Management/ArenaData/EthernalMenagerieData")]
    public class EternalMenagerieData : ArenaData
    {
        #region Members

        [SerializeField, Tooltip("Spell data of the Spawner for the Menagerie")]
        protected SpawnerData m_SpawnerData;
        
        [SerializeField]
        List<SWaveGroup> m_WaveGroups;

        public SpawnerData SpawnerData => m_SpawnerData;
        public SWaveGroup CurrentWaves => GetWavesAt(CurrentLevel);

        #endregion


        #region Accessors

        public SpawnerData GetSpawnerData(int? level = null)
        {
            if (level == null)
                level = CurrentBaseCharacterLevel;

            var spawnerData = m_SpawnerData.Clone(level.Value);
            spawnerData.SetWaves(CurrentWaves);

            return spawnerData;
        }

        public SWaveGroup GetWavesAt(int arenaLevel)
        {
            if (m_WaveGroups.IsNullOrEmpty())
            {
                ErrorHandler.Error($"No WaveGroups defined for Arena {ArenaType} at difficulty {ArenaDifficulty}");
                return default;
            }
            if (arenaLevel < 0 || m_WaveGroups.Count <= arenaLevel)
            {
                ErrorHandler.Error($"No waves found for arena level {arenaLevel} in arena {ArenaType} at difficulty {ArenaDifficulty}");
                return default;
            }

            return m_WaveGroups[arenaLevel];
        }

        #endregion
    }
}
