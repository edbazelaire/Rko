using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellRequirement
{
    [Serializable]
    public class SStateEffectRequirement : SRequirement
    {
        #region Members

        [SerializeField] protected string   m_StateEffect;
        [SerializeField] protected int      m_Stacks;
        [SerializeField] protected float    m_NStacksReductionPerLevel;

        public string StateEffect   => m_StateEffect;
        public int Stacks           => Math.Max(0, m_Stacks - (int)Math.Floor(m_Level * m_NStacksReductionPerLevel));

        public override string GetDescription()
        {
            return "Consumes " + Stacks + " stacks of [" + m_StateEffect + "]";
        }

        #endregion


        #region Checkers

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public override bool CheckRequirement(Controller targetController)
        {
            base.CheckRequirement(targetController);
            return targetController.StateHandler.GetStacks(m_StateEffect) >= m_Stacks;
        }

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public override bool TryApplyRequirements(Controller targetController)
        {
            if (!base.TryApplyRequirements(targetController))
                return false;

            targetController.StateHandler.RemoveStateEffect(m_StateEffect, true, m_Stacks);
            return true;
        }

        #endregion
    }
}