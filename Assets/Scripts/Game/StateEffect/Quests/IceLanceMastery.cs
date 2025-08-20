using Enums;
using Game.Spells;
using Tools;
using UnityEngine;


namespace Game.StateEffects.Quests
{
    [CreateAssetMenu(fileName = "IceLanceMastery", menuName = "Game/StateEffects/Quests/IceLanceMastery")]
    public class IceLanceMastery : QuestEffect
    {
        #region Listeners

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

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, int stacks, ulong targetId, ulong casterId, string origin)
        {
            // SAFETY : has a caster provided
            if (m_Caster == null)
            {
                ErrorHandler.Error("Provided Controller is null for state effect : " + stateEffectName + " - at event " + stateEffectEvent);
                return;
            }

            // SAFETY : is still active
            if (! m_IsActivated)
            {
                return;
            }

            // CHECK : comes from the correct caster
            if (casterId != m_Caster.PlayerId)
                return;

            // CHECK : does the provided stateEffect have one of activation effect
            if (stateEffectName != EStateEffect.IceBreak.ToString())
                return;

            // CHECK : the event is the required one
            if (stateEffectEvent != EStateEffectEvent.OnApplied)
                return;

            // CHECK : must be applied by a specific spell - IceLance
            if (! origin.Contains(ESpell.IceLance.ToString()))
                return;

            // add one stack
            Refresh(1);
        }

        #endregion
    }
}