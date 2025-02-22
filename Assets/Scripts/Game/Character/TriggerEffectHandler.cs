using Assets.Scripts.Data.PowerUp;
using Data;
using Data.DataStructures;
using Enums;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Character
{
    public class TriggerEffectHandler : NetworkBehaviour
    {
        #region Members

        public Action<string, int> QuestValueChanged;

        protected List<STriggerEffect>      m_TriggerEffects;
        protected List<PowerEffectData>     m_PowerEffects;     // TODO : Remove if not used (replacement to TriggerEffects)

        protected bool                      m_IsActivated = false;
        protected Controller                m_Controller;

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
        }

        public void Initialize(List<STriggerEffect> triggerEffects) 
        {
            m_TriggerEffects = triggerEffects;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (!IsServer || m_TriggerEffects == null)
                return;

            foreach(var triggerEffect in m_TriggerEffects)
            {
                triggerEffect.End();
            }

            // Clear the collection to remove all trigger effects
            m_TriggerEffects.Clear();
        }

        #endregion


        #region Activation

        public virtual void Activate(bool activate)
        {
            if (m_Controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return;
            }

            if (activate)
            {
                OnGameStartEffect();
                RegisterListeners();
            }
            else
            {
                UnRegisterListeners();
            }

            m_IsActivated = activate;
        }

        #endregion


        #region Add / Remove

        public void AddPowerUp(SRunePower runePower)
        {
            if (!IsServer)
                return;

            AddTriggerEffects(runePower.TriggerEffects);
        }

        public void AddTriggerEffects(List<STriggerEffect> triggerEffects)
        {
            if (triggerEffects.Count == 0)
                return;

            // add provided list of trigger effects to total list of trigger effects
            m_TriggerEffects.AddRange(triggerEffects);

            // check if has instant activation
            foreach (var triggerEffect in triggerEffects)
            {
                CheckOnGameStartEffect(triggerEffect);
            }
        }

        public void RemoveTriggerEffect(STriggerEffect triggerEffect)
        {
            if (m_TriggerEffects.Contains(triggerEffect))
                m_TriggerEffects.Remove(triggerEffect);
        }

        #endregion


        #region OnGameStart & OnDeath

        protected virtual void OnGameStartEffect()
        {
            if (!IsServer)
                return;

            foreach (var effect in m_TriggerEffects)
            {
                CheckOnGameStartEffect(effect);
            }
        }

        protected virtual void CheckOnGameStartEffect(STriggerEffect effect)
        {
            // =================================================================================================
            // Spell Activation
            if (effect.SpellActivationEvent == ESpellActivation.GameStart)
            {
                effect.Activate(m_Controller);
            }
            else if (effect.SpellActivationEvent == ESpellActivation.Hp && effect.ActivationTreshold >= (float)m_Controller.Life.Hp.Value / m_Controller.Life.MaxHp.Value)
            {
                effect.Activate(m_Controller);
            }
            else if (effect.SpellActivationEvent == ESpellActivation.Shield && effect.ActivationTreshold == 1 && m_Controller.Life.FinalShield.Value > 0)
            {
                effect.Activate(m_Controller);
            }
        }

        public virtual bool OnDeathEffect()
        {
            if (!IsServer)
                return false;

            bool success = false;
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                var effect = m_TriggerEffects[i];

                // CHECK : is triggered by death
                if (effect.SpellActivationEvent != ESpellActivation.Death)
                    continue;

                // CHECK : can be activated
                if (! effect.IsActivable())
                    continue;

                // set hp tp 1 before activating effects (Life.IsAlive beeing false can cause issue)
                m_Controller.Life.Hp.Value = 1;

                effect.Activate(m_Controller);
                m_TriggerEffects[i] = effect;
                success = true;

                Debug.LogWarning("      + Trigerred DeathEffect : " + effect);
            }

            return success;
        }

        #endregion


        #region Listeners

        protected void RegisterListeners()
        {
            if (!IsServer)
                return;

            m_Controller.Life.Hp.OnValueChanged             += OnHpChanged;
            m_Controller.Life.FinalShield.OnValueChanged    += OnShieldChanged;
        }

        protected void UnRegisterListeners()
        {
            if (!IsServer)
                return;

            m_Controller.Life.Hp.OnValueChanged             -= OnHpChanged;
            m_Controller.Life.FinalShield.OnValueChanged    -= OnShieldChanged;
        }

        protected virtual void OnHpChanged(int oldValue, int newValue)
        {
            // TRIGGER EFFECTS
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                var effect = m_TriggerEffects[i];
                if (
                    ! effect.IsActivated
                    && effect.SpellActivationEvent == ESpellActivation.Hp 
                    && effect.ActivationTreshold >= (float)newValue / m_Controller.Life.MaxHp.Value
                )
                {
                    effect.Activate(m_Controller);
                    m_TriggerEffects[i] = effect;
                }

                if (
                    effect.IsActivated 
                    && effect.SpellDeactivationEvent == ESpellActivation.Hp 
                    && effect.DeactivationTreshold <= (float)newValue / m_Controller.Life.MaxHp.Value
                )
                {
                    effect.Deactivate();
                    m_TriggerEffects[i] = effect;
                }
            }
        }

        protected virtual void OnShieldChanged(int oldValue, int newValue)
        {
            // TRIGGER EFFECTS
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                var effect = m_TriggerEffects[i];

                // CHECK : Activation
                if (! effect.IsActivated)
                {
                    if (effect.SpellActivationEvent == ESpellActivation.Shield && newValue <= effect.ActivationTreshold)
                    {
                        effect.Activate(m_Controller);
                        m_TriggerEffects[i] = effect;
                    }
                }

                // CHECK : Deactivation
                else
                {
                    if (effect.SpellDeactivationEvent == ESpellActivation.Shield && newValue >= effect.DeactivationTreshold)
                    {
                        effect.Deactivate();
                        m_TriggerEffects[i] = effect;
                    }
                }
            }
        }

        #endregion
    }
}