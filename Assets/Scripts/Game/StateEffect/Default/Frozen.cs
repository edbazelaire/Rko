using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Frozen", menuName = "Game/StateEffects/Frozen")]
    public class Frozen : StateEffect
    {
        public override void Update()
        {
            base.Update();

            if (! m_IsActivated || ! m_IsStarted)
                return;

            if (m_RemainingShield <= 0)
                m_Controller.StateHandler.RemoveStateEffect(StateEffectName);
        }

        /// <summary>
        /// Check that Frost state is applied, otherwise apply it instead of Frozen state
        /// </summary>
        /// <returns></returns>
        protected override bool CheckBeforeGraphicInit()
        {
            // can only apply to enemy with state "Frost"
            if (! m_Controller.StateHandler.HasState(EStateEffect.Frost))
            {
                // add frost state
                m_Controller.StateHandler.AddStateEffect(EStateEffect.Frost.ToString(), m_Caster, m_Level, m_Origin);
                return false;
            }

            // consume "Frost" state to apply "Frozen" state
            m_Controller.StateHandler.RemoveStateEffect(EStateEffect.Frost);

            return true;
        }

        public override void Refresh(int stacks = 0, int level = 1)
        {
            // Frozen cant be refreshed by another "Frozen" effect
            if (stacks > 0)
                return;

            base.Refresh(stacks, level);
        }
    }
}