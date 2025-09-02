using Assets.Scripts.Data.DataStructures.SpellRequirement;
using Data;
using Enums;
using Game.Character;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "TickDamageEffect", menuName = "Game/StateEffects/TickDamage")]
    public class TickDamageEffect : DamageEffect
    {
        #region Members

        [Header("Tick")]
        [SerializeField] protected float                    m_Tick;
        [SerializeField] protected int                      m_TickDamage;
        [SerializeField] protected int                      m_TickHeal;
        [SerializeField] protected int                      m_TickShield;
        [SerializeField] protected int                      m_TickEnergy;
        [SerializeField] protected List<SpellRequirements>  m_TickSpellRequirements;

        private float m_TickTimer;

        protected float FinalTickLifeSteal => GetFloat(EStateEffectProperty.LifeSteal, specialCondition: StateEffectName) + Mathf.Max(0f, m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusTickLifeSteal, m_Controller, specialCondition: StateEffectName) - 1);

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
            ErrorHandler.Log($"Applying {name} with " + m_Stacks + " stacks", ELogTag.StateEffects);

            // check if has spell requirements that need to be applied when applying effect
            if (! TryApplySpellRequirements(m_TickSpellRequirements))
                return;

            // ask state handler to fire the "OnHit" event to clients GFX
            CallStateEffectEvent(EStateEffectEvent.OnTick, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);

            // CHECK : DAMAGES
            int damages = GetInt(EStateEffectProperty.TickDamage);
            if (damages > 0)
            {
                if (m_Controller.CounterHandler.CheckCounters(damages, m_Caster, spellCategory: ESpellCategory.Tick))
                    return;

                ErrorHandler.Log($"{name} : {damages} DAMAGES", ELogTag.StateEffects);
                damages = m_Controller.Life.Hit(damages, m_Caster.PlayerId, m_Parent, ESpellCategory.Tick, m_IsTrueDamage);

                int lifesteal = (int)Mathf.Round(damages * FinalTickLifeSteal);
                if (lifesteal > 0)
                {
                    ErrorHandler.Log($"{name} : {lifesteal} LIFESTEAL", ELogTag.StateEffects);
                    m_Caster.Life.Heal(lifesteal, m_Caster.PlayerId, m_Parent, ESpellCategory.Tick);
                }
            }

            // CHECK : HEAL
            int heal = GetInt(EStateEffectProperty.TickHeal);
            if (heal > 0)
            {
                ErrorHandler.Log($"{name} : {heal} HEALS", ELogTag.StateEffects);
                m_Controller.Life.Heal(heal, m_Caster.PlayerId, m_Parent, ESpellCategory.Tick);
            }

            // add bonus tick shield
            m_Controller.Life.AddShield(GetInt(EStateEffectProperty.TickShield), m_Caster.PlayerId, m_Parent, ESpellCategory.Tick);

            // add bonus tick energy
            if (!m_Controller.CharacterData.IsStructure)
                m_Caster.EnergyHandler.AddEnergy(GetInt(EStateEffectProperty.TickEnergy));
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