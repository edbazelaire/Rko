using Assets.Scripts.Data.Interfaces;
using Data;
using Enums;
using System;
using Tools;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public class SForce
    {
        #region Members

        // =======================================================================
        // Serialized data
        [SerializeField]
        protected float m_Speed;
        [SerializeField]
        protected float m_Duration;

        // =======================================================================
        // Private
        protected int m_Level;
        protected float m_Timer;

        // =======================================================================
        // Public accessors
        public int          Level       => m_Level;
        public float        Speed       => m_Speed;
        public float        Duration    => m_Duration;

        public bool         IsActive    => Speed != 0;

        #endregion

        #region Clone & Level

        public void SetLevel(int level) 
        { 
            m_Level = level;
        }

        #endregion
    }
}