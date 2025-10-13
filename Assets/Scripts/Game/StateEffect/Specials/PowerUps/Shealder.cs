using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Shealder", menuName = "Game/StateEffects/SpecialEffects/PowerUps/Shealder")]
    public class Shealder : StateEffect
    {
        #region Members

        [Header("Holy Spirit")]
        [SerializeField, Tooltip("Percentage of Healing converted into Shield")]
        protected SScalingStat m_HealToShield;

        #endregion


        #region Level

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_HealToShield.SetLevel(level);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_Controller != null)
                m_Controller.Life.OnHealedEvent += OnHealedEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Controller != null)
                m_Controller.Life.OnHealedEvent -= OnHealedEvent;
        }

        private void OnHealedEvent(int heal, ulong casterId)
        {
            // convert heal to damages
            if (m_Controller == null || m_Controller.IsActive == false || GameManager.IsGameOver)
                return;

            m_Controller.Life.AddShield(
                m_Controller.StateHandler.ApplyBonusShield(
                    (int)Math.Round(heal * m_HealToShield.GetValue()),
                    m_Controller
                ),
                m_Controller.PlayerId,
                source: m_Origin,
                EHitCategory.Direct
            );
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[HealToShield]", Math.Round(m_HealToShield.GetValue() * 100).ToString() + "%");
            return description;
        }

        #endregion
    }
}