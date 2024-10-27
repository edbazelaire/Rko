using Data;
using Enums;
using Game.Spells;
using Game;
using System.Linq;
using Tools;
using UnityEngine;
using System.ComponentModel;
using System.Collections;
using System;

namespace Assets.Scripts.Data.PowerUp
{
    [CreateAssetMenu(fileName = "ActivationPowerUp", menuName = "Game/Effects/PowerUp/Activation")]
    public class ActivationPowerUpData : PowerUpData
    {
        #region Members

        [SerializeField] 
        protected SpellData             m_SpellData;
        [SerializeField] 
        protected StateEffect           m_StateEffectData;

        [SerializeField] 
        protected ESpellActivation      m_SpellActivationEvent;
        [SerializeField] 
        protected float                 m_ActivationTreshold;
        [SerializeField] 
        protected ESpellActivation      m_SpellDeactivationEvent;
        [SerializeField] 
        protected float                 m_DeactivationTreshold;

        [SerializeField, Description("Event from the requested State proccing the effect of this power up")]
        protected EStateEffectEvent     m_StateEffectEvent;
        [SerializeField, Description("Name of the state effect that procs the event")]
        protected string                m_StateEffectName;

        [SerializeField, Description("Number of times this effect can be activated")] 
        protected int                   m_NActivations;
        [SerializeField, Description("Cooldown between each activation")]
        protected float                 m_Cooldown;

        public SpellData            SpellData   => m_SpellData;

        public ESpellActivation     SpellActivationEvent    => m_SpellActivationEvent;
        public float                ActivationTreshold      => m_ActivationTreshold;
        public ESpellActivation     SpellDeactivationEvent  => m_SpellActivationEvent;
        public float                DeactivationTreshold    => m_DeactivationTreshold;

        public EStateEffectEvent    StateEffectEvent        => m_StateEffectEvent;
        public string               StateEffectName         => m_StateEffectName;

        public int                  NActivations            => m_NActivations;
        public float                Cooldown                => m_Cooldown;

        // ==================================================================================
        // DATA
        int                         m_NActivationsCtr;
        float                       m_CooldownTimer;

        #endregion


        #region Activation

        public override void Initialize(Controller controller)
        {
            base.Initialize(controller);

            RegisterActivation();
        }

        protected override void RegisterActivation()
        {
            if (m_Controller == null) 
            {
                ErrorHandler.Error("Trying to register activation for a PowerUp without Controller : " + Name);
                return;
            }


            switch (m_SpellActivationEvent)
            {
                case ESpellActivation.Hp:
                    m_Controller.Life.Hp.OnValueChanged += OnHpChanged;
                    break;

                case ESpellActivation.Shield:
                    m_Controller.Life.FinalShieldChangedEvent += OnShieldChanged;
                    break;

                default:
                    base.RegisterActivation();
                    return;
            }
        }

        public bool IsActivable()
        {
            return (NActivations == -1          // infinite activations
                || m_NActivationsCtr < (NActivations >= 1 ? NActivations : 1)) // OR below min activation 
            && m_CooldownTimer <= 0;            // cooldown must be done
        }

        public override void Activate()
        {
            base.Activate();

            if (m_IsActivated)
                return;

            if (m_Controller == null)
            {
                ErrorHandler.Error("Provided Controller is null for " + Name);
                return;
            }

            m_IsActivated = true;

            if (StateEffectEvent == EStateEffectEvent.None)
            {
                ActivateEffect(CalculateTarget());
                return;
            }

            StateEffect.StateEffectEvent += OnStateEffectEvent;
        }

        void ActivateEffect(Controller controller)
        {
            if (!IsActivable())
                return;

            if (controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return;
            }

            m_NActivationsCtr++;
            if (Cooldown > 0)
            {
                m_CooldownTimer = Cooldown;
                controller.StartCoroutine(UpdateCooldownTimer());
            }

            if (m_SpellData != null)
            {
                controller.StartCoroutine(m_SpellData.Clone(Level).CastDelay(controller.PlayerId, Vector3.zero, recalculateTarget: true));
                foreach (SPrefabSpawn prefabSpawn in m_SpellData.SpellEventActions)
                {
                    if (prefabSpawn.GFXLifetime.StartSpellPart == ESpellEvent.OnCast)
                        prefabSpawn.Spawn(controller, m_SpellData, null);
                }
            }

            if (m_StateEffectData != null)
            {
                controller.StateHandler.AddStateEffect(m_StateEffectData.Clone(Level), m_Controller);
            }
        }

        #endregion


        #region Deactivation / End

        public override void End()
        {
            base.End();
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (!m_IsActivated)
                return;

            m_IsActivated = false;
            StateEffect.StateEffectEvent -= OnStateEffectEvent;

            if (m_Controller == null)
            {
                ErrorHandler.Error("Unable to find controller when deactivating " + Name);
                return;
            }

            if (m_StateEffectData != null)
                m_Controller.StateHandler.RemoveStateEffect(m_StateEffectData.StateEffectName);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Controller.Life.Hp.OnValueChanged -= OnHpChanged;
            m_Controller.Life.FinalShieldChangedEvent -= OnShieldChanged;

            if (StateEffectEvent != EStateEffectEvent.None)
            {
                StateEffect.StateEffectEvent -= OnStateEffectEvent;
            }
        }

        #endregion


        #region Helpers

        IEnumerator UpdateCooldownTimer()
        {
            while (m_CooldownTimer > 0)
            {
                m_CooldownTimer -= Time.deltaTime;
                yield return null;
            }
        }

        bool HasStateEffect(string stateEffectName)
        {
            if (!StateEffectName.Contains(","))
            {
                return StateEffectName == stateEffectName;
            }

            return StateEffectName.Split(",").Contains(stateEffectName);
        }

        #endregion


        #region Listeners

        void OnStateEffectEvent(string stateEffectName, EStateEffectEvent stateEffectEvent, ulong targetId, ulong casterId)
        {
            if (m_Controller == null)
            {
                ErrorHandler.Error("Provided Controller is null for state effect : " + stateEffectName + " - at event " + stateEffectEvent);
                return;
            }

            if (!HasStateEffect(stateEffectName))
                return;

            if (stateEffectEvent != StateEffectEvent)
                return;

            if (casterId != m_Controller.PlayerId)
                return;

            ActivateEffect(CalculateTarget(targetId));
        }

        protected virtual void OnHpChanged(int oldValue, int newValue)
        {
            if (SpellActivationEvent == ESpellActivation.Hp && ActivationTreshold >= (float)newValue / m_Controller.Life.MaxHp.Value)
            {
                Activate();
            }

            else if (SpellDeactivationEvent == ESpellActivation.Hp && DeactivationTreshold >= (float)newValue / m_Controller.Life.MaxHp.Value)
            {
                Deactivate();
            }
        }

        protected virtual void OnShieldChanged(int oldValue, int newValue)
        {
            if (SpellActivationEvent == ESpellActivation.Shield && ((ActivationTreshold == 1 && newValue > 0) || (ActivationTreshold == 0 && newValue <= 0)))
            {
                Activate();
            }

            else if (SpellDeactivationEvent == ESpellActivation.Shield && ((DeactivationTreshold == 1 && newValue > 0) || (DeactivationTreshold == 0 && newValue <= 0)))
            {
                Deactivate();
            }
        }

        #endregion
    }
}