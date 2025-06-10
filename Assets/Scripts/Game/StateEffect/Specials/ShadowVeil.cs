using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "ShadowVeil", menuName = "Game/StateEffects/SpecialEffects/ShadowVeil")]
    public class ShadowVeil : StateEffect
    {
        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            StateEffect.StateEffectEvent += OnStateEffectEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            StateEffect.StateEffectEvent -= OnStateEffectEvent;
        }

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, int nStacks, ulong targetId, ulong casterId, string origin)
        {
            if (targetId != m_Controller.PlayerId)
                return;

            if (!m_IsActivated)
                return;

            if (
                m_Controller.StateHandler.IsStunned
                || m_Controller.StateHandler.IsAirborned
                || m_Controller.StateHandler.HasState(EStateEffect.Frozen)
            )
                End();
        }
    }
}