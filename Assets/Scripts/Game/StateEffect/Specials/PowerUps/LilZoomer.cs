using Assets.Scripts.Data.DataStructures.Common;
using Data;
using Data.GameManagement;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "LilZoomer", menuName = "Game/StateEffects/SpecialEffects/PowerUps/LilZoomer")]
    public class LilZoomer : StateEffect
    {
        #region Members

        [Header("LilZoomer")]
        [SerializeField, Tooltip("Threshold of size to reach")]
        protected float m_SizeThreshold = 0.2f;
        [SerializeField, Tooltip("Max speed bonus obtainable")]
        protected float m_MaxSpeedBonus = 2f;
        [SerializeField, Tooltip("Distance to reach to get a heal")]
        protected float m_DistanceThreshold = 2f;

        bool m_IsThresholReached = false;
        float m_DistanceRan = 0f;

        #endregion


        #region Activation / Deactivation

        public override void Activate()
        {
            base.Activate();

            m_IsThresholReached = false;
            RecalculateMS();
        }

        #endregion


        #region Udpate

        public override void Update()
        {
            if (!GameManager.Exists || m_Controller == null)
                return;

            base.Update();

            if (m_Controller.Movement.IsMoving)
                m_DistanceRan += Time.deltaTime * m_Controller.Movement.Speed;

            if (m_DistanceRan > m_DistanceThreshold)
            {
                m_DistanceRan -= m_DistanceThreshold;
                m_Controller.Life.Heal(GetInt(EStateEffectProperty.Heal), m_Controller.PlayerId, m_Origin, EHitCategory.Direct);
            }
        }

        #endregion


        #region Recalculate Bonus Values

        void RecalculateMS()
        {
            var bonusStats = m_BonusStats.FirstOrDefault(t => t.StateEffectProperty == EStateEffectProperty.SpeedBonus);
            if (bonusStats == default)
            {
                ErrorHandler.Warning("Unable to find the bonus stats for BonusDamagePerc in " + name);
                return;
            }

            bonusStats.BaseValue = Mathf.Max(1 - m_Controller.StateHandler.Size, 0) * m_MaxSpeedBonus;
            m_Controller.StateHandler.RecalculateBonus();
        }

        void CheckSizeThreshold()
        {
            if (m_Controller.StateHandler.Size > m_SizeThreshold || m_IsThresholReached)
                return;

            m_IsThresholReached = true;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            if (!GameManager.Exists || m_Controller == null)
                return;

            base.RegisterListeners();

            m_Controller.StateHandler.SizeBonus.OnValueChanged  += OnSizeChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (!GameManager.Exists || m_Controller == null)
                return;

            m_Controller.StateHandler.SizeBonus.OnValueChanged  -= OnSizeChanged;
        }

        void OnSizeChanged(float _, float newValue)
        {
            if (m_IsThresholReached)
                return;

            RecalculateMS();
            CheckSizeThreshold();
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[MaxSpeedBonus]", m_MaxSpeedBonus.ToString());

            int heal = GetInt(EStateEffectProperty.Heal);
            if (heal > 0)
                description += "\nYou heal " + heal + " while running.";

            return description;
        }

        #endregion
    }
}