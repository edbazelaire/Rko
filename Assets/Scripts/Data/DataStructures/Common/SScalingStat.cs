using System;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.Common
{
    [Serializable]
    public class SScalingStat
    {
        [SerializeField] float m_BaseValue      = 0f;
        [SerializeField] float m_ScalingPerc    = 0f;
        [SerializeField] float m_ScalingFix     = 0f;

        int m_Level;

        public int Level => m_Level;

        public SScalingStat(float baseValue, float scalingPerc = 0f, float scalingFix = 0f, int level = 0)
        {
            m_BaseValue     = baseValue;
            m_ScalingPerc   = scalingPerc;
            m_ScalingFix    = scalingFix;

            m_Level = level;
        }

        public float GetValue()
        {
            return m_BaseValue * Mathf.Pow(1 + m_ScalingPerc, m_Level) + m_ScalingFix * m_Level;
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