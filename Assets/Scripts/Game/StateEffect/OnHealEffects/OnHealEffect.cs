using Assets.Scripts.Data.DataStructures.Common;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.StateEffects.Interfaces;
using System;
using System.Collections.Generic;
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
        /// <summary> Convert stats into  </summary>
        public List<STargetStats> Stats;
    }

    [CreateAssetMenu(fileName = "OnHealEffect", menuName = "Game/StateEffects/OnHealEffect")]
    public class OnHealEffect : StateEffect, IHealInterceptor
    {
        #region Members

        [SerializeField] protected List<SHealEventAction> m_HealConversion = new();

        #endregion

        void IHealInterceptor.OnPreHeal(ref int heal, ulong casterId)
        {
            // convert heal to damages
            GameManager.Instance.GetFirstEnemy(m_Controller.Team).Life.Hit(
                damage:         (int)Math.Round(0.5f * heal), 
                casterId:       m_Controller.PlayerId, 
                source:         StateEffectName, 
                spellCategory:  ESpellCategory.Tick, 
                ignoreRes:      true
            );

            // reduce healing by 50%
            heal = (int)Math.Ceiling(0.5f * heal);
        }
    }
}