using Enums;
using System;
using Tools;
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

        public string StateEffect                   => m_StateEffect;
        public int Stacks                           => Math.Max(0, m_Stacks - (int)Math.Floor(m_Level * m_NStacksReductionPerLevel));
        public EScalingDirection ScalingDirection   => m_NStacksReductionPerLevel < 0 ? EScalingDirection.Up : (m_NStacksReductionPerLevel > 0 ? EScalingDirection.Down : EScalingDirection.None);

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
            return targetController.StateHandler.GetStacks(m_StateEffect) >= Stacks;
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

            targetController.StateHandler.RemoveStateEffect(m_StateEffect, consume: true, maxStacks: Stacks);
            return true;
        }

        #endregion


        #region Info & Description

        public override string GetDescription()
        {
            return "Consumes " + TextHandler.FormatScaling(Stacks.ToString(), ScalingDirection) + " stacks of [" + m_StateEffect + "]";
        }

        #endregion
    }
}