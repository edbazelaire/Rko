using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Corrupted", menuName = "Game/StateEffects/Corrupted")]
    public class Corrupted : TickDamageEffect
    {
        #region Constructor


        #endregion


        #region Inherited Manipulators

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_Controller == null)
                return;

            // if still has VoidPact : remove it
            if (m_Controller.StateHandler.HasState(EStateEffect.VoidPact))
                m_Controller.StateHandler.RemoveStateEffect(EStateEffect.VoidPact);
        }

        /// <summary>
        /// Check that Frost state is applied, otherwise apply it instead of Frozen state
        /// </summary>
        /// <returns></returns>
        protected override bool CheckBeforeGraphicInit()
        {
            // check enemy controller
            var enemyController = GameManager.Instance.GetFirstEnemy(m_Caster.Team);

            // can only apply to enemy with state "Burn"
            if (! enemyController.StateHandler.HasState(m_ConsumeState))
            {
                m_Stacks = 1;
            } else
            {
                m_Stacks = Math.Min(enemyController.StateHandler.RemoveStateEffect(m_ConsumeState), m_MaxStacks);
            }

            Debug.LogWarning("CURSED applied with " + m_Stacks + " stacks");
            m_Controller.StateHandler.AddStateEffect(EStateEffect.VoidPact, m_Controller, duration: m_Duration);

            return true;
        }

        #endregion
    }
}