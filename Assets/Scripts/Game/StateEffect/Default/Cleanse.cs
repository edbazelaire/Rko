using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Cleanse", menuName = "Game/StateEffects/Cleanse")]
    public class Cleanse : StateEffect
    {
        public virtual EStateEffect[] CLEANSEABLE_STATES => new EStateEffect[] { 
            EStateEffect.Silence,
            EStateEffect.Stun,
            EStateEffect.Frost,
            EStateEffect.Frozen,
            EStateEffect.Burn,
            EStateEffect.Scorched,
            EStateEffect.Cursed,
            EStateEffect.Malediction,
            EStateEffect.CorruptedPower,
            EStateEffect.Poison,
            EStateEffect.Infected,
        };

        protected override void OnStart()
        {
            foreach (var state in CLEANSEABLE_STATES)
            {
                if (! m_Controller.StateHandler.HasState(state))
                    continue;

                m_Controller.StateHandler.RemoveStateEffect(state, consume: false, m_Stacks);
            }

            base.OnStart();
        }
    }
}