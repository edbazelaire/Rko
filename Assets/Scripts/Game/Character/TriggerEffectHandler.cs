using Data.DataStructures;
using Enums;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;

namespace Game.Character
{
    public class TriggerEffectHandler : NetworkBehaviour
    {
        #region Members

        protected List<STriggerEffect> m_TriggerEffects;

        protected bool m_IsActivated = false;
        protected Controller m_Controller;

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
            } else
            {
                UnRegisterListeners();
            }

            m_IsActivated = activate;
        }

        #endregion


        #region OnGameStart & OnDeath

        protected virtual void OnGameStartEffect()
        {
            if (!IsServer)
                return;

            foreach (var effect in m_TriggerEffects)
            {
                if (effect.SpellActivationEvent == ESpellActivationEvent.GameStart)
                {
                    effect.Activate(m_Controller);
                }

                else if (effect.SpellActivationEvent == ESpellActivationEvent.Hp && effect.ActivationTreshold >= (float)m_Controller.Life.Hp.Value / m_Controller.Life.MaxHp.Value)
                {
                    effect.Activate(m_Controller);
                }

                else if (effect.SpellActivationEvent == ESpellActivationEvent.Shield && effect.ActivationTreshold == 1 && m_Controller.Life.Shield.Value > 0)
                {
                    effect.Activate(m_Controller);
                }
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
                if (effect.SpellActivationEvent != ESpellActivationEvent.Death)
                    continue;

                if (! effect.IsActivable())
                    continue;

                // set hp tp 1 before activating effects (Life.IsAlive beeing false can cause issue)
                m_Controller.Life.Hp.Value = 1;

                effect.Activate(m_Controller);
                m_TriggerEffects[i] = effect;
                success = true;                
            }

            return success;
        }

        #endregion


        #region Listeners

        protected void RegisterListeners()
        {
            if (!IsServer)
                return;

            bool linkedToHp = false;
            bool linkedToShield = false;
            foreach (var effect in m_TriggerEffects)
            {
                // make sure to only link once to HP and only link if necessary
                if ((effect.SpellActivationEvent == ESpellActivationEvent.Hp || effect.SpellActivationEvent == ESpellActivationEvent.Hp) && !linkedToHp)
                {
                    m_Controller.Life.Hp.OnValueChanged += OnHpChanged;
                    linkedToHp = true;
                }

                // make sure to only link once to SHIELD and only link if necessary
                if ((effect.SpellActivationEvent == ESpellActivationEvent.Shield || effect.SpellActivationEvent == ESpellActivationEvent.Shield) && !linkedToShield)
                {
                    m_Controller.Life.Shield.OnValueChanged                     += OnShieldChanged;
                    m_Controller.StateHandler.RemainingShield.OnValueChanged    += OnShieldChanged;
                    linkedToShield = true;
                }
            }
        }

        protected void UnRegisterListeners()
        {
            if (!IsServer)
                return;

            m_Controller.Life.Hp.OnValueChanged                         -= OnHpChanged;
            m_Controller.Life.Shield.OnValueChanged                     -= OnShieldChanged;
            m_Controller.StateHandler.RemainingShield.OnValueChanged    -= OnShieldChanged;
        }

        protected virtual void OnHpChanged(int oldValue, int newValue)
        {
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                var effect = m_TriggerEffects[i];
                if (effect.SpellActivationEvent == ESpellActivationEvent.Hp && effect.ActivationTreshold >= (float)newValue / m_Controller.Life.MaxHp.Value)
                {
                    effect.Activate(m_Controller);
                    m_TriggerEffects[i] = effect;
                }

                if (effect.SpellDeactivationEvent == ESpellActivationEvent.Hp && effect.DeactivationTreshold >= (float)newValue / m_Controller.Life.MaxHp.Value)
                {
                    effect.Deactivate();
                    m_TriggerEffects[i] = effect;
                }
            }
        }

        protected virtual void OnShieldChanged(int oldValue, int newValue)
        {
            int currentShield = m_Controller.Life.Shield.Value + m_Controller.StateHandler.RemainingShield.Value;
            for (int i = 0; i < m_TriggerEffects.Count; i++)
            {
                var effect = m_TriggerEffects[i];
                if (effect.SpellActivationEvent == ESpellActivationEvent.Shield && ((effect.ActivationTreshold == 1 && currentShield > 0) || (effect.ActivationTreshold == 0 && currentShield <= 0)))
                {
                    effect.Activate(m_Controller);
                    m_TriggerEffects[i] = effect;
                }

                if (effect.SpellDeactivationEvent == ESpellActivationEvent.Shield && ((effect.DeactivationTreshold == 1 && currentShield > 0) || (effect.DeactivationTreshold == 0 && currentShield <= 0)))
                {
                    effect.Deactivate();
                    m_TriggerEffects[i] = effect;
                }
            }
        }

        #endregion
    }
}