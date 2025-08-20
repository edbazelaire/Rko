using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "MoltenSpirit", menuName = "Game/StateEffects/SpecialEffects/MoltenSpirit")]
    public class MoltenSpirit : StateEffect
    {
        #region Members

        [SerializeField, Tooltip("Percentage of healing converted")]
        protected float m_HealConversionFactor = 0.15f;

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
            Refresh(stacks: Math.Min((int)Mathf.Round(heal * m_HealConversionFactor), m_MaxStacks), level: m_Level);
        }

        #endregion
    }
}