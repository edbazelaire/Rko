using Data.DataStructures.StateEffectSubStructures;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.Common
{
    [Serializable]
    public class STargetStats
    {
        [SerializeField] EStateEffectTarget m_Target    = 0f;
        [SerializeField] protected List<SBonusStats> m_BonusStats;

        int m_Level;

        public int Level => m_Level;

        public STargetStats(EStateEffectTarget target, List<SBonusStats> bonusStats = default, int level = 0)
        {
            m_Target        = target;
            m_BonusStats    = bonusStats;
            m_Level         = level;
        }

        public void SetLevel(int level)
        {
            if (level < 0)
            {
                ErrorHandler.Error("Level (" + level + ") is < 0");
                return;
            }

            m_Level = level;
        }
    }
}