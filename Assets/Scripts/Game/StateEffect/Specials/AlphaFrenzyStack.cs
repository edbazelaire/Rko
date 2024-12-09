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
            EStateEffect.Airborn.ToString(),
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

            m_Controller.StateHandler.StateEffectList.OnListChanged += OnStateEffectListChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Controller.StateHandler.StateEffectList.OnListChanged -= OnStateEffectListChanged;
        }

        void OnStateEffectListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            if (changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.Add)
                return;

            if (!REFRESH_EFFECTS.Contains(changeEvent.Value.ToString()))
                return;

            m_Stacks = 1;
            Refresh();

            m_Controller.StateHandler.OnStateEventClientRPC(EListEvent.Add, StateEffectName, Stacks, GetFloat(EStateEffectProperty.Duration));
        }

        #endregion
    }
}