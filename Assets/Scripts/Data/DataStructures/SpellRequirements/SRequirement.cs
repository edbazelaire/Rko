using Enums;
using Game;
using System;
using System.Collections.Generic;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellRequirement
{
    [Serializable]
    public class SRequirement
    {
        #region Members

        [SerializeField] protected ESpellTarget m_SpellTarget;
        protected int                           m_Level;

        public ESpellTarget     SpellTarget         => m_SpellTarget;
        public int              Level               => m_Level;

        #endregion


        #region Level

        public void SetLevel(int level)
        {
            m_Level = level;
        }

        #endregion


        #region Target

        public List<Controller> CalculateTargets(Controller caster, Controller targetController = null)
        {
            switch (m_SpellTarget)
            {
                case ESpellTarget.Self:
                    return new List<Controller>() { caster };

                case ESpellTarget.CurrentTarget:
                    if (targetController == null)
                    {
                        ErrorHandler.Error("SRequirement.CalculateTarget() - Current Target is required but is null");
                        return null;
                    }
                    return new List<Controller>() { targetController };

                case ESpellTarget.None:
                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetAllEnemies(caster.Team);

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetAllAllies(caster.Team);

                default: 
                    ErrorHandler.Error("SRequirement.CalculateTarget() - Unhandled case : " +  m_SpellTarget);
                    return null;
            }
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public virtual bool CheckRequirement(List<Controller> targetControllers)
        {
            return true;
        }

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public virtual bool TryApplyRequirements(List<Controller> targetControllers)
        {
            if (! CheckRequirement(targetControllers))
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