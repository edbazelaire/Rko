using System;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellRequirement
{
    [Serializable]
    public class SRequirement
    {
        #region Members

        protected int m_Level;

        public int Level            => m_Level;

        #endregion


        #region Level

        public void SetLevel(int level)
        {
            m_Level = level;
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public virtual bool CheckRequirement(Controller targetController)
        {
            return true;
        }

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public virtual bool TryApplyRequirements(Controller targetController)
        {
            if (!CheckRequirement(targetController))
                return false;

            return true;
        }


        #endregion


        #region Info & Description

        public virtual string GetDescription()
        {
            return "";
        }

        #endregion
    }
}