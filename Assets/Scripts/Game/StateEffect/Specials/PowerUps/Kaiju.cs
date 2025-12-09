using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Kaiju", menuName = "Game/StateEffects/SpecialEffects/PowerUps/Kaiju")]
    public class Kaiju : StateEffect
    {
        #region Members

        [Header("Kaiju")]
        [SerializeField, Tooltip("Threshold of size to reach to enable the effect")]
        protected float m_SizeThreshold = 5f;
        [SerializeField, Tooltip("Threshold of size to reach to enable the effect")]
        protected float m_HpToSizeConversion = 0.0005f;
        [SerializeField, Tooltip("Percentage of Size converted into Bonus (Damage | Heal) %")]
        protected float m_SizeToDamageConversion = 25f;
        
        [SerializeField, Tooltip("Tick")]
        protected float m_Tick = 3f;
        [SerializeField, Tooltip("Percentage of Hp converted into this attack tick damage")]
        protected float m_TickDamageHpPerc= 0.01f;

        float m_TickTimer = 0f;
        bool m_IsThresholReached = false;   

        #endregion


        #region Activation / Deactivation

        protected override void OnActivated()
        {
            base.OnActivated();

            m_IsThresholReached = false;
            RecalculateDamage();
            RecalculateHeal();
            RecalculateSize();
        }

        #endregion


        #region Update

        public override void Update()
        {
            if (!GameManager.IsGameRunning)
                return;

            if (!m_Controller.IsActive || !m_Controller.Life.IsAlive)
                return;

            base.Update();

            if (m_TickTimer <= 0f)
            {
                CheckCollisions();
                m_TickTimer = m_Tick;
            }

            m_TickTimer -= Time.deltaTime;
        }

        protected virtual void CheckCollisions()
        {
            // setup layer filter 
            var filter = Physics2DQueries.BuildFilter(TargetHelper.DEFAULT_LAYER_MASK);

            // Check for collisions within a circle with variableRadius radius
            Collider2D[] hits = new Collider2D[32];
            int count = m_Controller.Collider.Overlap(filter, hits);

            // Gat all controllers touched by the 2D collision circle
            var hitControllers = new List<Controller>();
            for (int i = 0; i < count; i++)
            {
                if (!CheckCollision(hits[i], out Controller controller))
                    continue;

                hitControllers.Add(controller);
            }

            // Apply OnHit effect on each Controllers
            foreach (var controller in hitControllers)
            {
                controller.Life.Hit(
                    (int)Math.Round(m_Controller.Life.MaxHp.Value * m_TickDamageHpPerc), 
                    casterId:       m_Controller.PlayerId, 
                    source:         m_Origin,
                    damageCategory: EDamageCategory.Magical,
                    hitCategory:    EHitCategory.Direct
                );
            }
        }


        /// <summary>
        /// 
        /// </summary>q
        /// <param name="collision"></param>
        protected virtual bool CheckCollision(Collider2D collision, out Controller controller)
        {
            // check that players has controller 
            controller = Finder.FindComponent<Controller>(collision.gameObject);
            if (controller == null)
            {
                ErrorHandler.Error("Controller not found for player " + collision.gameObject.name);
                return false;
            }

            if (controller.Team == m_Controller.Team)
                return false;

            if (!controller.IsActive)
                return false;

            return true;
        }


        #endregion


        #region Recalculate Bonus Values

        void RecalculateSize()
        {
            var bonusStat = m_BonusStats.FirstOrDefault(t => t.StateEffectProperty == EStateEffectProperty.Size);
            if (bonusStat == default)
            {
                ErrorHandler.Warning("Unable to find the bonus stats for Size in " + name);
                return;
            }

            bonusStat.BaseValue = m_HpToSizeConversion * m_Controller.Life.MaxHp.Value;
            ErrorHandler.Log(() => $"{StateEffectName} - {bonusStat.GetKeyName()} : {bonusStat.Get(m_Level, m_Stacks):F2}", ELogTag.StatConversion);

            m_Controller.StateHandler.RecalculateBonus();
        }

        void RecalculateDamage()
        {
            var bonusStat = m_BonusStats.FirstOrDefault(t => t.StateEffectProperty == EStateEffectProperty.Power);
            if (bonusStat == default)
            {
                ErrorHandler.Warning("Unable to find the bonus stats for BonusDamagePerc in " + name);
                return;
            }

            bonusStat.BaseValue = 100 * m_SizeToDamageConversion * m_Controller.StateHandler.Size;
            ErrorHandler.Log(() => $"{StateEffectName} - {bonusStat.GetKeyName()} : {bonusStat.Get(m_Level, m_Stacks):F2}", ELogTag.StatConversion);
        }

        void RecalculateHeal()
        {
            var bonusStat = m_BonusStats.FirstOrDefault(t => t.StateEffectProperty == EStateEffectProperty.BonusHealPerc);
            if (bonusStat == default)
            {
                ErrorHandler.Warning("Unable to find the bonus stats for BonusHealPerc in " + name);
                return;
            }

            bonusStat.BaseValue = m_SizeToDamageConversion * m_Controller.StateHandler.Size;
            ErrorHandler.Log(() => $"{StateEffectName} - {bonusStat.GetKeyName()} : {bonusStat.Get(m_Level, m_Stacks):F2}", ELogTag.StatConversion);
        }

        void CheckSizeThreshold()
        {
            if (m_Controller.StateHandler.Size < m_SizeThreshold || m_IsThresholReached)
                return;

            m_IsThresholReached = true;
            m_Controller.StateHandler.AddStateEffect(EStateEffect.Uncontrollable, m_Controller, m_Origin, 1, -1);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_Controller == null)
                return;

            m_Controller.Life.MaxHp.OnValueChanged              += OnHpChanged;
            m_Controller.StateHandler.SizeBonus.OnValueChanged  += OnSizeChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Controller == null)
                return;

            m_Controller.Life.MaxHp.OnValueChanged              -= OnHpChanged;
            m_Controller.StateHandler.SizeBonus.OnValueChanged  -= OnSizeChanged;
        }

        void OnHpChanged(int _, int newValue)
        {
            if (m_IsThresholReached)
                return;

            RecalculateSize();
        }

        void OnSizeChanged(float _, float newValue)
        {
            if (m_IsThresholReached)
                return;

            RecalculateDamage();
            RecalculateHeal();
            CheckSizeThreshold();
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            var power = m_BonusStats.FirstOrDefault(t => t.StateEffectProperty == EStateEffectProperty.Power);
            power.BaseValue = m_SizeToDamageConversion * 100;

            string description = base.GetDescription();
            description = description.Replace("[SizeThreshold]", m_SizeThreshold.ToString());
            description = description.Replace("[HpToSizeConversion]", (m_HpToSizeConversion * 100).ToString("F2") + "%");
            description = description.Replace("[SizeToDamageConversion]", Math.Round(m_SizeToDamageConversion * 100).ToString() + "%");
            description = description.Replace("[BonusPower]", power.Get(m_Level, 1).ToString());
            description = description.Replace("[TickDamageHpPerc]", Math.Round(m_TickDamageHpPerc * 100).ToString() + "%");
            return description;
        }

        #endregion
    }
}