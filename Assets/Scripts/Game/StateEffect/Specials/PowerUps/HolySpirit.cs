using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "HolySpirit", menuName = "Game/StateEffects/SpecialEffects/PowerUps/HolySpirit")]
    public class HolySpirit : StateEffect
    {
        #region Members

        [Header("Holy Spirit")]
        [SerializeField, Tooltip("Threshold of healing to reach to enable the effect")]
        protected SScalingStat m_HealThreshold;
        [SerializeField, Tooltip("Number of stacks of Cleanse applied on reaching threshold")]
        protected SScalingStat m_CleanseStacks;

        #endregion


        #region Level

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_HealThreshold.SetLevel(level);
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

            Refresh(heal);

            if (m_Stacks < m_HealThreshold.GetValue())
                return;

            while (m_Stacks >= m_HealThreshold.GetValue())
            {
                RemoveStacks((int)Math.Round(m_HealThreshold.GetValue()));

                m_Controller.StateHandler.AddStateEffect(
                    EStateEffect.Cleanse, 
                    m_Controller, 
                    origin: StateEffectName, 
                    stacks: (int)Math.Round(m_CleanseStacks.GetValue()),
                    level: m_Level
                );
            }
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[HealThreshold]", Math.Round(m_HealThreshold.GetValue()).ToString());
            description = description.Replace("[CleanseStacks]", Math.Round(m_CleanseStacks.GetValue()).ToString());
            return description;
        }

        #endregion
    }
}