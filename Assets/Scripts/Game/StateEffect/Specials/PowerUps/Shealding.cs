using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Shealding", menuName = "Game/StateEffects/SpecialEffects/PowerUps/Shealding")]
    public class Shealding : StateEffect
    {
        #region Members

        [Header("Shealding")]
        [SerializeField, Tooltip("Duration of one tick")]
        protected float m_Tick;
        [SerializeField, Tooltip("Percentage of Shield converted into Healing")]
        protected SScalingStat m_ShieldToHeal;

        float m_TickTimer;

        #endregion


        #region Setup

        protected override void ApplyPreProcessing()
        {
            base.ApplyPreProcessing();
            m_TickTimer = m_Tick;
        }

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_ShieldToHeal.SetLevel(level);
        }

        #endregion


        #region Update

        public override void Update()
        {
            base.Update();

            if (m_Controller.Life.FinalShield.Value <= 0)
                return;

            if (m_TickTimer > 0)
            {
                m_TickTimer -= Time.deltaTime;
                return;
            }

            m_TickTimer = m_Tick;

            m_Controller.Life.Heal(
                m_Controller.StateHandler.ApplyBonusHealDealt(
                    (int)Math.Round(m_Controller.Life.FinalShield.Value * m_ShieldToHeal.GetValue()),
                    m_Controller
                ),
                m_Controller.PlayerId,
                source: m_Origin,
                EHitCategory.Dot
            );
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[ShieldToHeal]", Math.Round(m_ShieldToHeal.GetValue() * 100).ToString() + "%");
            return description;
        }

        #endregion
    }
}