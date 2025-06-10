using Enums;
using Game.Spells;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public class SConditionTrigger
    {
        #region Members

        // ==================================================================================
        // Actions
        public Action<bool> IsTrueChangedEvent;

        // ==================================================================================
        // Serialize fields
        public ETriggerType     Trigger;
        public EComparator      Comparator;
        public float            Value;
        public bool             IsPercValue;

        // ==================================================================================
        // Private Variables
        public bool? IsTrue     { get; private set; }
        Controller              m_Controller;

        #endregion


        #region Check & Register

        void SetIsTrue(bool isTrue)
        {
            if (IsTrue == isTrue)
                return;

            IsTrue = isTrue;

            // call event that the "isTrue" value has changed
            IsTrueChangedEvent?.Invoke(IsTrue.Value);
        }

        public bool Check(float value)
        {
            switch (Comparator)
            {
                case EComparator.Inf:   return value < Value;
                case EComparator.InfEq: return value <= Value;
                case EComparator.Equal: return value == Value;
                case EComparator.SupEq: return value >= Value;
                case EComparator.Sup:   return value > Value;

                default:
                    ErrorHandler.Error("Unahandled case : " + Comparator);
                    return false;
            }
        }

        public void Register(Controller controller)
        {
            m_Controller = controller;

            switch (Trigger)
            {
                case ETriggerType.None:
                    ErrorHandler.Warning($"Trying to register ConditionTrigger with TriggerType == None");
                    return;

                case ETriggerType.Hp:
                    // -- first check to see if this is "activated" or not
                    OnHpChanged(m_Controller.Life.Hp.Value, m_Controller.Life.Hp.Value);
                    controller.Life.Hp.OnValueChanged += OnHpChanged;
                    return;

                case ETriggerType.Shield:
                    // -- first check to see if this is "activated" or not
                    OnShieldChanged(m_Controller.Life.FinalShield.Value, m_Controller.Life.FinalShield.Value);
                    controller.Life.FinalShield.OnValueChanged += OnShieldChanged;
                    return;

                default:
                    ErrorHandler.Warning("Unhandled case : " + Trigger);
                    return;
            }
        }

        public void UnRegister()
        {
            switch (Trigger)
            {
                case ETriggerType.None:
                    ErrorHandler.Warning($"Trying to register ConditionTrigger with TriggerType == None");
                    return;

                case ETriggerType.Hp:
                    m_Controller.Life.Hp.OnValueChanged             -= OnHpChanged;
                    return;

                case ETriggerType.Shield:
                    m_Controller.Life.FinalShield.OnValueChanged    -= OnShieldChanged;
                    return;
            }
        }

        #endregion


        #region Listeners

        void OnHpChanged(int oldValue, int newValue)
        {
            float value;
            if (IsPercValue)
                value = (float)newValue / m_Controller.Life.MaxHp.Value;
            else
                value = newValue;

            bool isTrue = Check(value);
            if (IsTrue == isTrue)
                return;

            SetIsTrue(isTrue);
        }

        void OnShieldChanged(int oldValue, int newValue)
        {
            SetIsTrue(Check(newValue));
        }

        #endregion
    }


    [Serializable]
    public struct SStateEffectAction
    {
        public EStateEffectEvent    StateEffectEvent;
        public string               Value;

        #region Activation / Deactivation

        public void Activate(StateEffect stateEffect)
        {
            int nStacks;

            switch (StateEffectEvent)
            {
                case EStateEffectEvent.OnActivated:
                    stateEffect.Activate();
                    break;

                case EStateEffectEvent.OnDeactivated:
                    stateEffect.Deactivate();
                    break;

                case EStateEffectEvent.OnEnd:
                    stateEffect.End();
                    break;

                case EStateEffectEvent.OnRefreshed:
                    if (!int.TryParse(Value, out nStacks))
                    {
                        ErrorHandler.Error($"Unable to parse number of stacks ({Value}) of {stateEffect.StateEffectName} into an integer");
                        return;
                    }
                    stateEffect.Refresh(nStacks);
                    break;

                case EStateEffectEvent.OnRemoved:
                    if (! int.TryParse(Value, out nStacks))
                    {
                        ErrorHandler.Error($"Unable to parse number of stacks ({Value}) of {stateEffect.StateEffectName} into an integer");
                        return;
                    }
                    stateEffect.RemoveStacks(nStacks, consume: false);
                    break;

                case EStateEffectEvent.OnConsumed:
                    if (! int.TryParse(Value, out nStacks))
                    {
                        ErrorHandler.Error($"Unable to parse number of stacks ({Value}) of {stateEffect.StateEffectName} into an integer");
                        return;
                    }
                    stateEffect.RemoveStacks(nStacks, consume: true);
                    break;

                default:
                    ErrorHandler.Error($"Unhandled case ({StateEffectEvent}) for effect {stateEffect.StateEffectName}");
                    break;
            }
        }

        /// <summary>
        /// Reverse the activation effect
        /// </summary>
        /// <param name="stateEffect"></param>
        public void Deactivate(StateEffect stateEffect)
        {
            switch (StateEffectEvent)
            {
                case EStateEffectEvent.OnActivated:
                    stateEffect.Deactivate();
                    break;

                case EStateEffectEvent.OnDeactivated:
                    stateEffect.Activate();
                    break;

                default:
                    ErrorHandler.Error($"Unhandled DEACTIVATE case ({StateEffectEvent}) for effect {stateEffect.StateEffectName}");
                    break;
            }
        }

        #endregion
    }


    [Serializable]
    public class SStateEffectTrigger
    {
        #region Members

        [Tooltip("Reverse the effect when the set of conditions goes from True to False")]
        public bool                     ReverseEffect;
        public List<SConditionTrigger>  ConditionTriggers;
        public SStateEffectAction       StateEffectAction;

        // ===================================================================================================
        // Private Members
        protected bool          m_IsActivated = false;
        protected Controller    m_Controller;
        protected StateEffect   m_StateEffect;

        #endregion


        #region Activation / Deactivation

        void Activate(bool activate)
        {
            if (activate == m_IsActivated)
                return;

            m_IsActivated = activate;

            if (activate)
            {
                StateEffectAction.Activate(m_StateEffect);
            } else if (ReverseEffect)
            {
                StateEffectAction.Deactivate(m_StateEffect);
            }
        }

        #endregion


            #region Listeners

        public void Register(Controller controller, StateEffect stateEffect)
        {
            m_Controller = controller;
            m_StateEffect = stateEffect;

            for (int i = 0; i < ConditionTriggers.Count; i++)
            {
                SConditionTrigger conditionTrigger = ConditionTriggers[i];
                conditionTrigger.IsTrueChangedEvent += OnIsTrueChanged;
                conditionTrigger.Register(controller);

                ConditionTriggers[i] = conditionTrigger;
            }
        }

        public void UnRegister()
        {
            for (int i = 0; i < ConditionTriggers.Count; i++)
            {
                SConditionTrigger conditionTrigger = ConditionTriggers[i];
                conditionTrigger.UnRegister();
                conditionTrigger.IsTrueChangedEvent -= OnIsTrueChanged;
            }
        }

        public void OnIsTrueChanged(bool isTrue)
        {
            // already in the state - no need to check all conditions
            if (isTrue == m_IsActivated) 
                return;

            foreach (var condition in ConditionTriggers)
            {
                if (!condition.IsTrue.HasValue)
                    return;

                if (! condition.IsTrue.Value)
                {
                    Activate(false);
                    return;
                }
            }

            Activate(true);
        }

        #endregion


    }

}