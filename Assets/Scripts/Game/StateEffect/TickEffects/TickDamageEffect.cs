using Assets.Scripts.Data.DataStructures.SpellRequirement;
using Data;
using Enums;
using Game.Character;
using Game.UI;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "TickDamageEffect", menuName = "Game/StateEffects/TickDamage")]
    public class TickDamageEffect : DamageEffect
    {
        #region Members

        [Header("Tick")]
        [SerializeField] protected float                    m_Tick;
        [FormerlySerializedAs("m_TickDamage")]
        [SerializeField] protected int                      m_DotDamage;
        [FormerlySerializedAs("m_TickHeal")]
        [SerializeField] protected int                      m_DotHeal;
        [FormerlySerializedAs("m_TickShield")]
        [SerializeField] protected int                      m_DotShield;
        [FormerlySerializedAs("m_TickEnergy")]
        [SerializeField] protected int                      m_DotEnergy;
        [SerializeField] protected List<SStateEffectData>   m_TickEffects;
        [SerializeField] protected List<SpellRequirements>  m_TickSpellRequirements;

        private float m_TickTimer;

        protected float FinalTickLifeSteal => GetFloat(EStateEffectProperty.LifeSteal, specialCondition: StateEffectName) + Mathf.Max(0f, m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal, m_Controller, hitCategory: EHitCategory.Dot, specialCondition: StateEffectName) - 1);

        #endregion


        #region Init & End

        public override bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffect, int? stacks = null)
        {
            if (!base.Initialize(controller, caster, stateEffect, stacks))
                return false;

            m_TickTimer = m_Tick;
            return true;
        }

        #endregion


        #region Update

        public override void Update()
        {
            base.Update();

            if (m_IsHolding)
                return;

            m_TickTimer -= Time.deltaTime;

            if (m_TickTimer > 0 || m_Tick <= 0)
                return;

            ApplyTickEffects();
            m_TickTimer = m_Tick;
        }

        #endregion


        #region Apply Effect
        
        protected bool TryApplySpellRequirements(List<SpellRequirements> allSpellRequirements)
        {
            if (allSpellRequirements.Count == 0)
                return true;

            // if at least one is true, requirements are met
            foreach (var spellRequirements in allSpellRequirements)
            {
                if (spellRequirements.TryApplyRequirements(m_Caster, m_Controller)) 
                    return true;
            }

            // none of the requirements met : failure
            return false;
        }

        protected virtual void ApplyTickEffects()
        {
            ErrorHandler.Log(() => $"Applying {name} with " + m_Stacks + " stacks", ELogTag.StateEffects);

            // check if has spell requirements that need to be applied when applying effect
            if (! TryApplySpellRequirements(m_TickSpellRequirements))
                return;

            // ask state handler to fire the "OnHit" event to clients GFX
            CallStateEffectEvent(EStateEffectEvent.OnTick, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);

            // CHECK : DAMAGES
            int damages = GetInt(EStateEffectProperty.DotDamage);
            if (damages > 0)
            {
                ErrorHandler.Log(() => $"{name} : {damages} DAMAGES", ELogTag.StateEffects);
                damages = m_Controller.Life.Hit(damages, m_Caster.PlayerId, m_Parent, EDamageCategory.Magical, EHitCategory.Dot, m_IsTrueDamage);

                int lifesteal = (int)Mathf.Round(damages * FinalTickLifeSteal);
                if (lifesteal > 0)
                {
                    ErrorHandler.Log(() => $"{name} : {lifesteal} LIFESTEAL", ELogTag.StateEffects);
                    m_Caster.Life.Heal(lifesteal, m_Caster.PlayerId, m_Parent, EHitCategory.Dot);
                }
            }

            // CHECK : HEAL
            int heal = GetInt(EStateEffectProperty.DotHeal);
            if (heal > 0)
            {
                ErrorHandler.Log(() => $"{name} : {heal} HEALS", ELogTag.StateEffects);
                m_Controller.Life.Heal(heal, m_Caster.PlayerId, m_Parent, EHitCategory.Dot);
            }

            // add bonus tick shield
            m_Controller.Life.AddShield(GetInt(EStateEffectProperty.DotShield), m_Caster.PlayerId, m_Parent, EHitCategory.Dot);

            // add bonus tick energy
            if (!m_Controller.CharacterData.IsStructure)
                m_Controller.EnergyHandler.AddEnergy(GetInt(EStateEffectProperty.DotEnergy));

            // apply OnTick effects
            ApplyOnTickEffects();
        }

        void ApplyOnTickEffects()
        {
            if (!m_Controller.Life.IsAlive)
                return;

            if (m_Controller.StateHandler == null)
                return;

            foreach (var effect in m_TickEffects)
            {
                m_Controller.StateHandler.AddStateEffect(
                    effect: effect.StateEffect, 
                    caster: m_Caster.IsSpawn ? m_Caster.SpawnOwner : m_Caster, 
                    origin: m_Parent, 
                    stacks: (int)Math.Round(m_Stacks * (effect.Stacks + Math.Floor(m_Level * effect.BonusStacksPerLevel)))
                );
            }
        }

        #endregion


        #region Description & Info

        public override string GetDescription()
        {
            return base.GetDescription();
        }

        #endregion
    }
}