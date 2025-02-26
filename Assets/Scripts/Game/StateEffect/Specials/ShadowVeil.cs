using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "ShadowVeil", menuName = "Game/StateEffects/SpecialEffects/ShadowVeil")]
    public class ShadowVeil : StateEffect
    {
        protected override void OnStart()
        {
            base.OnStart();

            m_Controller.StateHandler.StateEffectListEvent += OnStateChanged;
        }

        void OnStateChanged(EListEvent listEvent, string name, int nStacks, float value)
        {
            if (m_IsActivated && (m_Controller.StateHandler.IsStunned || m_Controller.StateHandler.IsAirborned))
                Deactivate();
            else if (!m_IsActivated && !(m_Controller.StateHandler.IsStunned || m_Controller.StateHandler.IsAirborned))
                Activate();
        }
    }
}