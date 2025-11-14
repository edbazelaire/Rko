using Data;
using Enums;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SoulFeast", menuName = "Game/StateEffects/SpecialEffects/SoulFeast")]
    public class SoulFeast : StateEffect
    {
        #region Members

        [SerializeField] float m_HpToEnergyConversion = 0.1f;
        [SerializeField] float m_HpToShieldConversion = 0.1f;

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            Controller.OnDeathEvent += OnDeathEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            Controller.OnDeathEvent -= OnDeathEvent;
        }

        void OnDeathEvent(Controller controller)
        {
            if (controller.Team != m_Controller.Team || controller == m_Controller)
                return;

            m_Controller.EnergyHandler.AddEnergy((int)Math.Round(controller.Life.MaxHp.Value * m_HpToEnergyConversion));
            m_Controller.Life.AddShield((int)Math.Round(controller.Life.MaxHp.Value * m_HpToShieldConversion), m_Caster.PlayerId, StateEffectName, EHitCategory.Direct);
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            var description = base.GetDescription();
            description = description.Replace("[HpToEnergyConversion]", (m_HpToEnergyConversion * 100) + "%" );
            description = description.Replace("[HpToShieldConversion]", (m_HpToShieldConversion * 100) + "%" );
            return description;
        }

        #endregion
    }
}