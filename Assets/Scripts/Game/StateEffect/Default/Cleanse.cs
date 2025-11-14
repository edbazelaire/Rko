using Enums;
using Tools;
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

                int removedStacks = m_Controller.StateHandler.RemoveStateEffect(state, consume: false, m_Stacks);
                ErrorHandler.Log("Cleansing " + removedStacks + " stacks of " + state, ELogTag.StateEffects);
            }

            base.OnStart();
        }
    }
}