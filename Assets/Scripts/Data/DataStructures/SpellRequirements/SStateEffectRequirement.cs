using Enums;
using System;
using System.Collections.Generic;
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
        public override bool CheckRequirement(List<Controller> targetControllers)
        {
            if (!base.CheckRequirement(targetControllers))
                return false;

            int stacksCounter = 0;
            foreach ( Controller controller in targetControllers )
            {
                stacksCounter += controller.StateHandler.GetStacks(m_StateEffect);
                if (stacksCounter >= Stacks)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public override bool TryApplyRequirements(List<Controller> targetControllers)
        {
            if (!base.TryApplyRequirements(targetControllers))
                return false;

            int remainingStacks = Stacks;
            foreach (var targetController in targetControllers)
            {
                int removedStacks = targetController.StateHandler.RemoveStateEffect(m_StateEffect, consume: true, maxStacks: remainingStacks);
                remainingStacks -= removedStacks;

                if (remainingStacks < 0) { }
            }

            // CHECK : are there still stacks to remove ? This should not happen
            if (remainingStacks > 0)
                ErrorHandler.Warning("Has stacks remaining : " + remainingStacks);

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