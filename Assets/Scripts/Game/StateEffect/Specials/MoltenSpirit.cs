using Enums;
using Game.Character;
using Game.StateEffects.Interfaces;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "MoltenSpirit", menuName = "Game/StateEffects/SpecialEffects/MoltenSpirit")]
    public class MoltenSpirit : StateEffect
    {
        #region Members

        [SerializeField, Tooltip("Percentage of healing converted")]
        float m_HealConversionFactor = 0.1f;

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Controller.Life.OnHealedEvent += OnHealedEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Controller.Life.OnHealedEvent -= OnHealedEvent;
        }

        private void OnHealedEvent(int heal, ulong casterId)
        {
            int previousStacks = m_Stacks;
            Refresh(stacks: Math.Min(m_Stacks + (int)Mathf.Round(heal * m_HealConversionFactor), m_MaxStacks), level: m_Level);
        }

        #endregion
    }
}