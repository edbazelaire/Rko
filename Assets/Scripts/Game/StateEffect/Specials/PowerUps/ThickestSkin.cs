using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "ThickestSkin", menuName = "Game/StateEffects/SpecialEffects/PowerUps/ThickestSkin")]
    public class ThickestSkin : StateEffect
    {
        #region Members

        [Header("Shealding")]
        [SerializeField, Tooltip("Percentage of Shield converted into Healing")]
        protected SScalingStat m_HpToShield;

        #endregion


        #region Setup

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_HpToShield.SetLevel(level);
        }

        #endregion


        #region Listeners

        protected override void OnApplied(int stacks)
        {
            base.OnApplied(stacks);

            // Delay method (to be applied after all the Initial Boosts)
            CoroutineManager.DelayMethod(() =>
            {
                m_Controller.Life.AddShield(
                   m_Controller.StateHandler.ApplyBonusShield(
                       (int)Math.Round(m_Controller.Life.MaxHp.Value * m_HpToShield.GetValue()),
                       m_Controller
                   ),
                   m_Controller.PlayerId,
                   source: m_Origin,
                   EHitCategory.Direct
               );
            });
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[HpToShield]", Math.Round(m_HpToShield.GetValue() * 100).ToString() + "%");
            return description;
        }

        #endregion
    }
}