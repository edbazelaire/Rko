using Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "AlphaFrenzyStack", menuName = "Game/StateEffects/SpecialEffects/AlphaFrenzyStack")]
    public class AlphaFrenzyStack : StateEffect
    {
        #region Members

        /// <summary> list of effects that refreshes this  </summary>
        List<string> REFRESH_EFFECTS = new List<string>()
        {
            EStateEffect.Stun.ToString(),
            EStateEffect.Scorched.ToString(),
            EStateEffect.Frozen.ToString(),
            EStateEffect.Airborne.ToString(),
        };

        protected float m_StackRefreshTimer;

        #endregion


        #region Init & End

        protected override void OnStart()
        {
            base.OnStart();
        }

        public override void Refresh(int stacks = 0, int level = 1)
        {
            base.Refresh(stacks, level);

            if (m_Stacks >= m_MaxStacks)
                End();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_Controller == null)
                return;

            m_Controller.StateHandler.StateEffectEvent += OnStateEffectEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Controller == null)
                return;

            m_Controller.StateHandler.StateEffectEvent -= OnStateEffectEvent;
        }

        void OnStateEffectEvent(EStateEffectEvent stateEffectEvent, string stateEffectName, int nStacks, int maxStacks, float duration)
        {
            if (stateEffectEvent != EStateEffectEvent.OnApplied 
                && stateEffectEvent != EStateEffectEvent.OnRefreshed
                && stateEffectEvent != EStateEffectEvent.OnActivated)
                return;

            if (! REFRESH_EFFECTS.Contains(stateEffectName))
                return;

            SetStacks(1);
            Refresh();
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description += "\n\nAlpha Frenzy : " + m_OnEndStateEffect[0].GetDescription();
            return description;
        }

        #endregion
    }
}