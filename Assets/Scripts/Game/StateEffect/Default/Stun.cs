using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Stun", menuName = "Game/StateEffects/Stun")]
    public class Stun : StateEffect
    {
        public void Refresh(float duration, int stacks = 1)
        {
            //duration /= Mathf.Pow(2, m_Controller.StateHandler.CCCounter.Value);

            m_Duration += duration * stacks;
            m_Timer += duration * stacks;

            //m_Controller.StateHandler.CCCounter.Value += 1;

            CallStateEffectEvent(EStateEffectEvent.OnRefreshed, stacks, m_Controller.PlayerId, m_Caster.PlayerId);
        }
    }
}