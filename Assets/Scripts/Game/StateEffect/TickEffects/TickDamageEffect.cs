using Assets.Scripts.Data.DataStructures.SpellRequirement;
using Data;
using Enums;
using System.Collections;
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
        [SerializeField] protected int                      m_TickDamages;
        [SerializeField] protected int                      m_TickHeal;
        [SerializeField] protected int                      m_TickShield;
        [SerializeField] protected List<SpellRequirements>  m_TickSpellRequirements;

        private float m_TickTimer;

        #endregion


        #region Inherited Manipulators

        public override bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffect)
        {
            if (!base.Initialize(controller, caster, stateEffect))
                return false;

            m_TickTimer = m_Tick;
            return true;
        }

        public override void Update()
        {
            base.Update();

            m_TickTimer -= Time.deltaTime;

            if (m_TickTimer > 0 || m_Tick <= 0)
                return;

            ApplyTickEffects();
            m_TickTimer = m_Tick;
        }

        protected bool TryApplySpellRequirements(List<SpellRequirements> allSpellRequirements)
        {
            if (allSpellRequirements.Count == 0)
                return true;

            // if at least one is true, requirements are met
            foreach (var spellRequirements in allSpellRequirements)
            {
                if (spellRequirements.TryApplyRequirements(m_Controller)) 
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
            StateEffectEvent?.Invoke(StateEffectName, EStateEffectEvent.OnTick, m_Controller.PlayerId, m_Caster.PlayerId);
            m_Controller.StateHandler.CallSpellEventClientRPC(ESpellEvent.OnHit, StateEffectName);

            // CHECK : DAMAGES
            int damages = GetInt(EStateEffectProperty.TickDamages);
            if (damages > 0)
            {
                ErrorHandler.Log($"{name} : {damages} DAMAGES", ELogTag.StateEffects);
                m_Controller.Life.Hit(damages, true);

                int lifesteal = (int)Mathf.Round(damages * FinalLifeSteal);
                if (lifesteal > 0)
                {
                    ErrorHandler.Log($"{name} : {lifesteal} LIFESTEAL", ELogTag.StateEffects);
                    m_Caster.Life.Heal(lifesteal);
                }
            }

            // CHECK : HEAL
            int heal = GetInt(EStateEffectProperty.TickHeal);
            if (heal > 0)
            {
                ErrorHandler.Log($"{name} : {heal} HEALS", ELogTag.StateEffects);
                m_Controller.Life.Heal(heal);
            }

            // add bonus tick shield
            m_RemainingShield += GetInt(EStateEffectProperty.TickShield);
        }

        #endregion
    }
}