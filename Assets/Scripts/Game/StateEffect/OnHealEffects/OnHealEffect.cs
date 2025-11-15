using Assets.Scripts.Data.DataStructures.Common;
using Data;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.StateEffects.Interfaces;
using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.Helpers;
using UnityEngine;

namespace Game.Spells
{
    public enum EHealEvent
    {
        OnPreHeal,              // pure healing received before any reduction or excess removed
        OnHeal,                 // only affects the actual heal value (how much hp the character actually gained)
        OnExcessHealing,        // only affects the healing exceeding the max hp of the target
    }

    [Serializable]
    public struct SHealEventAction
    {
        /// <summary> Event whene this effect is proccing </summary>
        public EHealEvent   Event;
        /// <summary> Percentage of healing converted </summary>
        public float        HealConversion;
        /// <summary> How much is the heal used for "conversion" is going to be reduced </summary>
        public float        ConvertedHealReduction;
        /// <summary> Convert heals into stats </summary>
        public List<STargetStats> Stats;
        /// <summary> Convert heals into stateEffects </summary>
        public List<SStateEffectData> StateEffects;

        public void Apply(ref int heal, Controller controller, ulong casterId)
        {
            if (! Stats.IsNullOrEmpty())
            {
                foreach (var stat in Stats) 
                {
                    var target = TargetHelper.GetTargetController(casterId, stat.Target);
                    if (target == null)
                        continue;

                    // TODO     ---------------------
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "OnHealEffect", menuName = "Game/StateEffects/OnHealEffect")]
    public class OnHealEffect : StateEffect, IHealInterceptor
    {
        #region Members

        [SerializeField] protected List<SHealEventAction> m_HealEventActions = new();

        #endregion


        #region Init



        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_Controller == null)
                return;

            if (m_HealEventActions.Any(t => t.Event == EHealEvent.OnHeal))
                m_Controller.Life.OnHealedEvent += OnHeal;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            if (m_Controller == null)
                return;

            m_Controller.Life.OnHealedEvent -= OnHeal;
        }

        void IHealInterceptor.OnPreHeal(ref int heal, ulong casterId)
        {
            foreach (var healEventAction in m_HealEventActions)
            {
                if (healEventAction.Event == EHealEvent.OnPreHeal)
                    healEventAction.Apply(ref heal, m_Controller, casterId);
            }
        }

        void OnHeal(int heal, ulong casterId)
        {
            foreach (var healEventAction in m_HealEventActions)
            {
                if (healEventAction.Event == EHealEvent.OnHeal)
                    healEventAction.Apply(ref heal, m_Controller, casterId);
            }
        }

        #endregion

    }
}