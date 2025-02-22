using Enums;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
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

            m_Controller.StateHandler.StateEffectListEvent += OnStateEffectListEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Controller.StateHandler.StateEffectListEvent -= OnStateEffectListEvent;
        }

        void OnStateEffectListEvent(EListEvent listEvent, string stateEffect, int stacks, float duration)
        {
            if (listEvent != EListEvent.Add)
                return;

            if (! REFRESH_EFFECTS.Contains(stateEffect))
                return;

            m_Stacks = 1;
            Refresh();
        }

        #endregion
    }
}