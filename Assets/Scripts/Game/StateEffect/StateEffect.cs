using Assets.Scripts.Data.DataStructures;
using Assets.Scripts.Managers.Sound;
using Data;
using Enums;
using Game.Loaders;
using Game.SpellGFXs;
using Game.UI;
using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Spells
{
    [Serializable]
    public struct SStateEffectScaling
    {
        public EStateEffectProperty     StateEffectProperty;
        public float                    ScalingFactor;

        public SStateEffectScaling(EStateEffectProperty stateEffectProperty, float scalingFactor = 1.1f)
        {
            StateEffectProperty     = stateEffectProperty;
            ScalingFactor           = scalingFactor;
        }
    }

    [Serializable]
    public struct SBonusStats
    {
        public EStateEffectProperty StateEffectProperty;
        public float BaseValue;
        public float LevelScalingFactor;
        public float StackScalingFactor;

        public SBonusStats(EStateEffectProperty stateEffectProperty, float baseValue = 0f, float levelScalingFactor = 0.1f, float stackScalingFactor = 0.1f)
        {
            StateEffectProperty = stateEffectProperty;
            BaseValue           = baseValue;
            LevelScalingFactor  = levelScalingFactor;
            StackScalingFactor  = stackScalingFactor;
        }
    }

    [CreateAssetMenu(fileName = "StateEffect", menuName = "Game/StateEffects/Default")]
    [System.Serializable]
    public class StateEffect : ScriptableObject
    {
        #region Members

        /// <summary> Event fired at each state effect state </summary>
        public static Action<string, EStateEffectEvent, ulong, ulong> StateEffectEvent;

        // =========================================================================================
        // SERIALIZED DATA
        [SerializeField] protected      string                      m_Description = "";
        [SerializeField] protected      List<EStateEffectProperty>  m_DescriptionVariables = new List<EStateEffectProperty>();
        [SerializeField] protected      EStateEffectType            m_StateEffectType = EStateEffectType.Default;

        [Header("Graphics")]
        [SerializeField] protected      bool                        m_IsDisplayed = true;
        [SerializeField] protected      List<SpellPrefabSpawn>      m_VisualEffects;
        [SerializeField] protected      EAnimation                  m_Animation;
        [SerializeField] protected      AudioClip                   m_OnApplySoundFX;
        [SerializeField] protected      AudioClip                   m_PermanantSoundFX;

        [Header("Consume State")]
        [SerializeField] protected EStateEffect                     m_ConsumeState;
        [ConditionalField("ConsumeState", true, EStateEffect.None)]
        [SerializeField] protected EStateEffect                     m_DefaultState;
        [SerializeField] protected List<SStateEffectData>           m_SubStateEffects;
        [SerializeField] protected List<string>                     m_HoldingStateEffects;

        [Header("General Stats")]
        [SerializeField] protected      int                         m_Priority              = 0;
        [SerializeField] protected      float                       m_Duration              = 0f;
        [SerializeField] protected      bool                        m_IsInstantanious       = false;
        [SerializeField] protected      int                         m_MaxStacks             = 1;
        [SerializeField] protected      int                         m_StackDecay            = -1;   // number of stacks decaying at the end of the duration (-1 = all stacks)

        [Header("General Boosts")]
        [SerializeField] protected      List<SBonusStats>           m_BonusStats;
        [SerializeField] protected      float                       m_SpeedBonus            = 0f;
        [SerializeField] protected      float                       m_CastSpeed             = 0f;
        [SerializeField] protected      float                       m_AttackSpeed           = 0f;
        [SerializeField] protected      int                         m_CooldownReduction     = 0;
        [SerializeField] protected      float                       m_CooldownReductionPerc = 0f;

        [Header("Resistance & Shields")]
        [SerializeField] protected      int                         m_Shield                = 0;
        [SerializeField] protected      int                         m_ResistanceFix         = 0;
        [SerializeField] protected      float                       m_ResistancePerc        = 0f;

        [Header("Damages")]
        [SerializeField] protected      int                         m_BonusDamages          = 0;
        [SerializeField] protected      float                       m_BonusDamagesPerc      = 0f;
        [SerializeField] protected      float                       m_BonusLifeSteal        = 0f;

        [Header("Heal")]
        [SerializeField] protected      int                         m_BonusHeal             = 0;
        [SerializeField] protected      float                       m_BonusHealPerc         = 0f;

        [Header("Elementary")]
        [SerializeField] protected      int                         m_BonusBurnDamages      = 0;
        [SerializeField] protected      float                       m_BonusSlowPerc         = 0f;

        [Header("Extra Effects")]
        [SerializeField] protected List<StateEffect>                m_OnStartStateEffect    = new();
        [SerializeField] protected List<StateEffect>                m_OnEndStateEffect      = new();

        [Header("Level Scaling")]
        /// <summary> Scaling factor for each properties depending on number of Stacks for each levels </summary>
        [SerializeField] protected      List<SStateEffectScaling>   m_StateEffectScalingLevel   = new();

        /// <summary> Scaling factor for each properties depending on number of Stacks for each stats </summary>
        [SerializeField] protected      List<SStateEffectScaling>   m_StateEffectScalingStacks  = new();

        // =========================================================================================
        // PROTECTED MEMBERS   
        /// <summary> Controller on which the state effect is applied </summary>
        protected Controller            m_Controller;
        /// <summary> Controller that applied the state effect </summary>
        protected Controller            m_Caster;
        /// <summary> Level of the state effect </summary>
        protected int                   m_Level;
        /// <summary> Type of state effect </summary>
        protected EStateEffect          m_Type;
        /// <summary> source of audio </summary>
        protected AudioSource           m_AudioSource;

        protected int                   m_Stacks = 1;   
        protected int                   m_RemainingShield;
        protected bool                  m_IsHolding = false;
        protected float                 m_Timer;

        // =========================================================================================
        // ACTIONS
        public Action<ESpellEvent> OnSpellEvent;

        // =========================================================================================
        // DEPENDENT MEMBERS  
        public virtual EStateEffectType StateEffectType     => m_StateEffectType;
        public List<SStateEffectData>   SubStateEffects     => m_SubStateEffects;

        public bool                     IsUnique            => StateEffectType == EStateEffectType.Incarnation || StateEffectType == EStateEffectType.AutoAttackBuff;
        public List<SpellPrefabSpawn>   VisualEffects       => m_VisualEffects;
        public EAnimation               Animation           => m_Animation;
        public EStateEffect             Type                => Enum.TryParse(name, out EStateEffect type) ? type : m_Type ;
        public virtual int              Stacks              => m_Stacks;
        public virtual int              Priority            => m_Priority;
        public virtual bool             IsInfinite          => m_Duration <= 0;
        public int                      RemainingShield     => m_RemainingShield;
        public bool                     IsInstantanious     => m_IsInstantanious;
        public virtual int              MaxStacks           => m_MaxStacks;
        public EStateEffect             ConsumeState        => m_ConsumeState;
        public EStateEffect             DefaultState        => m_DefaultState;
        public int                      Level               => m_Level;
        public bool                     IsBuff              => SpellLoader.SpellExists(StateEffectName);

        public string StateEffectName
        {
            get
            {
                string myName = name;
                if (myName.EndsWith("(Clone)"))
                    myName = myName[..^"(Clone)".Length];

                return myName;
            }
        }

        #endregion


        #region Init & End

        public virtual bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffectData = null)
        {
            if (controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return false;
            }

            m_Controller = controller;
            m_Caster = caster;
            m_Stacks = stateEffectData.HasValue ? stateEffectData.Value.GetStacks() : 1;

            // check if has overriding data
            if (stateEffectData.HasValue && stateEffectData.Value.OverridingProperties != null && stateEffectData.Value.OverridingProperties.Count > 0)
                OverrideStateEffectData(stateEffectData.Value);

            if (!CheckBeforeGraphicInit())
            {
                return false;
            }

            RefreshStats();

            OnStart();

            // if spell is instantanious do not proceed after initalization in state handler
            if (m_IsInstantanious)
                return false;

            m_IsHolding = m_Controller.StateHandler.IsHolding(StateEffectName);

            RegisterListeners();

            return true;
        }

        #endregion


        #region At Init 

        /// <summary>
        /// Method that allows children to make a verification before instantiating this
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckBeforeGraphicInit()
        {
            if (m_ConsumeState == EStateEffect.None)
                return true;

            m_Stacks = Mathf.Min(ApplyConsumeState(m_Stacks), MaxStacks);
            return m_Stacks > 0;
        }

        protected virtual void OnStart()
        {
            ActivateHoldingStateEffects(true);

            ApplySubStateEffect();

            ApplyOnStartStateEffects();

            // call state effect 
            StateEffectEvent?.Invoke(StateEffectName, EStateEffectEvent.OnApplied, m_Controller.PlayerId, m_Caster.PlayerId);
            m_Controller.StateHandler.CallSpellEventClientRPC(ESpellEvent.OnSpawn, StateEffectName);

            // if instantatious effect : end after start
            if (m_IsInstantanious)
                End();
        }

        public void OverrideStateEffectData(SStateEffectData stateEffectData)
        {
            name = stateEffectData.StateEffect.ToString();

            foreach (SStateEffectProperty overridingProperty in stateEffectData.OverridingProperties)
            {
                SetProperty(overridingProperty.StateEffectProperty, overridingProperty.Value);
            }
        }

        public void PlaySoundEffect()
        {
            if (m_OnApplySoundFX != null)
                SoundFXManager.PlayOnce(m_OnApplySoundFX);

            if (m_PermanantSoundFX != null)
                m_AudioSource = SoundFXManager.PlaySoundFXClip(m_PermanantSoundFX);
        }

        #endregion


        #region End & Destroy

        /// <summary>
        /// Reached its end naturaly or was consumed by another spell
        /// </summary>
        public virtual void End()
        {
            if (! IsInstantanious)
                m_Controller.StateHandler.RemoveStateEffect(StateEffectName, true);

            ActivateHoldingStateEffects(false);
            StateEffectEvent?.Invoke(StateEffectName, EStateEffectEvent.OnRemoved, m_Controller.PlayerId, m_Caster.PlayerId);
        }

        public virtual void OnConsumed()
        {
            foreach (var stateEffect in m_OnEndStateEffect)
            {
                var clone = stateEffect.Clone(m_Level);
                m_Controller.StateHandler.AddStateEffect(clone, m_Caster);
            }

            // delay event of "OnConsumed" to wait for the end of the consumption
            string name = StateEffectName;
            ulong controllerId = m_Controller.PlayerId;
            ulong casterId = m_Caster.PlayerId;
            CoroutineManager.DelayMethod(() => StateEffectEvent?.Invoke(name, EStateEffectEvent.OnConsumed, controllerId, casterId));
        }

        protected virtual void OnDestroy()
        {
            if (m_AudioSource != null)
                Destroy(m_AudioSource);

            if (m_Controller == null)
                return;

            UnRegisterListeners();

            m_Controller.StateHandler.CallSpellEventClientRPC(ESpellEvent.OnEnd, StateEffectName);
        }

        #endregion


        #region Update & Refresh

        public virtual void Update()
        {
            if (IsInfinite)
                return;

            if (m_IsHolding)
                return;

            m_Timer -= Time.deltaTime;

            if (m_Timer <= 0)
            {
                // refresh timer
                m_Timer = m_Duration;

                // remove N stacks
                RemoveStacks(m_StackDecay);
            }
        }

        /// <summary>
        /// Refresh an effect and add N stacks. Also upgrade level if the spell that is refreshing the spell is higher level
        /// </summary>
        /// <param name="stacks"></param>
        /// <param name="level"></param>
        public virtual void Refresh(int stacks = 0, int level = 1)
        {
            StateEffectEvent?.Invoke(StateEffectName, EStateEffectEvent.OnRefreshed, m_Controller.PlayerId, m_Caster.PlayerId);

            // if stacks = 0 -> just refresh timer and leave
            if (stacks == 0)
            {
                RefreshStats();
                return;
            }

            // check state that needs to be consumed
            if (m_ConsumeState != EStateEffect.None)
            {
                // set refreshed stacks to number of consumed stacks
                stacks = ApplyConsumeState(stacks);
                if (stacks == 0)
                    return;     // no stacks consumed : do not refresh
            }

            // keep max level as applied level
            if (m_Level < level)
                SetLevel(level);

            m_Stacks = Math.Min(m_MaxStacks, m_Stacks + stacks);
            RefreshStats();
        }

        protected virtual void RefreshStats()
        {
            m_RemainingShield = GetInt(EStateEffectProperty.Shield); 
            m_Timer = m_Duration;
        }

        public virtual int RemoveStacks(int nStacks)
        {
            if (nStacks >= m_Stacks || nStacks <= 0)
            {
                nStacks = m_Stacks;
                End();
                return nStacks;
            }

            m_Stacks -= nStacks;

            // refresh UI on client side
            m_Controller.StateHandler.OnStateEventClientRPC(EListEvent.Add, StateEffectName, Stacks, GetFloat(EStateEffectProperty.Duration));
            return nStacks;
        }

        #endregion


        #region Activation & Application

        /// <summary>
        /// Check if player has required "ConsumeState"
        /// </summary>
        /// <returns></returns>
        protected virtual int ApplyConsumeState(int stacks = 1)
        {
            // if has state to consume 
            if (m_Controller.StateHandler.HasState(m_ConsumeState))
            {
                // consume "ConsumeState" state to apply current state
                return m_Controller.StateHandler.RemoveStateEffect(m_ConsumeState, true, stacks);
            }

            // add DefaultState state (if any)
            if (m_DefaultState != EStateEffect.None)
                m_Controller.StateHandler.AddStateEffect(m_DefaultState, m_Caster);

            // return that no stacks has been 
            return 0;
        }

        protected virtual void ActivateHoldingStateEffects(bool activate)
        {
            if (activate)
                m_Controller.StateHandler.AddHoldingStateEffects(m_HoldingStateEffects);
            else
                m_Controller.StateHandler.RemoveHoldingStateEffects(m_HoldingStateEffects);
        }

        void ApplySubStateEffect()
        {
            if (m_SubStateEffects == null)
                return;
            
            foreach (var subStateEffect in m_SubStateEffects)
            {
                m_Controller.StateHandler.AddStateEffect(subStateEffect, m_Controller, level: m_Level);
            }
        }

        protected virtual void ApplyOnStartStateEffects()
        {
            if (m_OnStartStateEffect == null)
                return;

            foreach (var stateEffect in m_OnStartStateEffect)
            {
                var clone = stateEffect.Clone(m_Level);
                clone.m_Duration = m_Duration;
                m_Controller.StateHandler.AddStateEffect(clone, m_Controller);
            }
        }

        #endregion


        #region Shield

        /// <summary>
        /// Hit the shield with some damages and return the remaining damages
        /// </summary>
        /// <param name="damages"></param>
        /// <returns></returns>
        public virtual int HitShield(int damages)
        {
            m_RemainingShield -= damages;
            if (m_RemainingShield >= 0)
                return 0;

            damages = -m_RemainingShield;
            m_RemainingShield = 0;
            return damages;
        }

        #endregion


        #region Level Scaling Methods

        public StateEffect Clone(int level)
        {
            StateEffect clone = Instantiate(this);
            clone.name = this.name;

            // make sure that original level is copied
            clone.m_Level = m_Level;
            clone.SetLevel(level);

            return clone;
        }

        protected void SetLevel(int level)
        {
            ApplyNewLevelFactorAll(level);
            m_Level = level;

            for (int i = 0; i < m_SubStateEffects.Count; i++)
            {
                var stateEffect = m_SubStateEffects[i];
                stateEffect.SetLevel(level);
                m_SubStateEffects[i] = stateEffect;
            }
        }

        protected virtual void ApplyNewLevelFactorAll(int newLevel)
        {
            foreach (SStateEffectScaling stateEffectScaling in m_StateEffectScalingLevel)
            {
                ApplyNewLevelScalingEffectFactor(stateEffectScaling, newLevel, m_Level);
            }
        }

        protected virtual void ApplyNewLevelScalingEffectFactor(SStateEffectScaling stateEffectScaling, int newLevel, int oldLevel)
        {
            // factor of current level
            float currentFactor = (float)Math.Pow(1 + stateEffectScaling.ScalingFactor, Math.Max(oldLevel - 1, 0));
            // factor of the level we are setting
            float newFactor = (float)Math.Pow(1 + stateEffectScaling.ScalingFactor, Math.Max(newLevel - 1, 0));

            // current factor is 0 -> set value to 0 to avoid division by 0
            if (currentFactor == 0)
            {
                ErrorHandler.Warning(string.Format("ScalingFactor ({0}) of property ({1}) of StateEffect {2} resulted in a division by 0", stateEffectScaling.ScalingFactor, stateEffectScaling.StateEffectProperty, name));
                SetProperty(stateEffectScaling.StateEffectProperty, 0);
                return;
            }

            // get info of the property
            if (!TryGetPropertyInfo(stateEffectScaling.StateEffectProperty, out FieldInfo propertyInfo))
                return;

            Type propertyType = propertyInfo.FieldType;
            if (propertyType == typeof(float))
            {
                var value = GetProperty<float>(stateEffectScaling.StateEffectProperty);
                SetProperty(stateEffectScaling.StateEffectProperty, value * newFactor / currentFactor);
                return;
            }

            if (propertyType == typeof(int))
            {
                var value = GetProperty<int>(stateEffectScaling.StateEffectProperty);
                SetProperty(stateEffectScaling.StateEffectProperty, (int)Math.Round(value * newFactor / currentFactor));
                return;
            }
        }

        #endregion


        #region Reflection Methods

        /// <summary>
        /// Check if state effect has expected property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public bool HasEffectProperty(EStateEffectProperty property)
        {
            if (! m_BonusStats.IsNullOrEmpty() && m_BonusStats.Any(value => value.StateEffectProperty == property))
                return true;

            return TryGetPropertyInfo(property, out _, throwError: false);
        }

        /// <summary>
        /// Get Reflection PropertyInfo of desire StateEffect property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected bool TryGetPropertyInfo(EStateEffectProperty property, out FieldInfo propertyInfo, bool throwError = true)
        {
            // Get the type of MyClass
            Type myStateEffectType = this.GetType();

            // Get the PropertyInfo object for the provided property
            propertyInfo = myStateEffectType.GetField("m_" + property.ToString(), BindingFlags.NonPublic | BindingFlags.Instance);

            // check if the property exists
            if (propertyInfo == null)
            {
                if (throwError)
                    ErrorHandler.Error("Unknown property " + property + " for StateEffect " + name);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set the value of a property by Reflection
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        protected virtual void SetProperty(EStateEffectProperty property, object value)
        {
            if (!TryGetPropertyInfo(property, out FieldInfo propertyInfo))
                return;

            propertyInfo.SetValue(this, value);
        }

        /// <summary>
        /// Get the value of a property by Reflection
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual object GetProperty(EStateEffectProperty property)
        {
            if (TryGetBonusStat(property, out float value))
                return value;

            if (!TryGetPropertyInfo(property, out FieldInfo propertyInfo))
                return null;

            return propertyInfo.GetValue(this);
        }

        protected virtual T GetProperty<T>(EStateEffectProperty property)
        {
            object value = GetProperty(property);
            if (value == null)
                return default;

            try
            {
                // Check if the object is already of the desired type
                if (value is T typedValue)
                    return typedValue;

                // Attempt to convert if it's a numeric type and `T` is numeric
                if (typeof(T) == typeof(int) && value is IConvertible)
                    return (T)(object)Convert.ToInt32(value);

                if (typeof(T) == typeof(float) && value is IConvertible)
                    return (T)(object)Convert.ToSingle(value);

                if (typeof(T) == typeof(double) && value is IConvertible)
                    return (T)(object)Convert.ToDouble(value);

                // Handle string to int or other parseable types
                if (value is string stringValue && typeof(T) == typeof(int))
                    return (T)(object)int.Parse(stringValue);

                // Add additional conversions as needed

                // If none of the conversions worked, try direct casting
                return (T)value;
            }
            catch (Exception ex)
            {
                ErrorHandler.Error(ex.Message);
                ErrorHandler.Error("Unable to parse value " + value + " of property " + property + " of state effect " + name + " into " + typeof(T));
                return default;
            }
        }

        #endregion


        #region Data Accessors

        public virtual bool TryGetBonusStat(EStateEffectProperty property, out float value)
        {
            value = 0f;
            
            // check if bonus stats provided
            if (m_BonusStats.IsNullOrEmpty())
                return false;
            
            // check that bonus stats has requested value
            if (! m_BonusStats.Any(value => value.StateEffectProperty == property))
                return false;

            // get bonus stats
            SBonusStats bonusStats = m_BonusStats.FirstOrDefault(value => value.StateEffectProperty == property);

            // calculate scaling factors (levels and stacks)
            float levelFactor = (float)Math.Pow(1 + bonusStats.LevelScalingFactor, Math.Max(Level - 1, 0));
            float stacksFactor = (float)Math.Pow(1 + bonusStats.StackScalingFactor, Math.Max(Stacks, 0));

            value = bonusStats.BaseValue * levelFactor;
            if (m_Controller == null)
                return true;

            value = m_Controller.StateHandler.ApplyBonus(value, property, null);    // Bonus values applied to the property
            value *= Stacks * stacksFactor;                                         // apply Stack bonus 
            return true;
        }

        public virtual int GetInt(EStateEffectProperty property) 
        {
            if (TryGetBonusStat(property, out float value))
                return (int)Mathf.Round(value);

            SStateEffectScaling stateEffectScalingStacks = m_StateEffectScalingStacks.FirstOrDefault(effect => effect.StateEffectProperty == property);

            int baseValue = GetProperty<int>(property);                                             // Base Value of the property

            if (baseValue == 0)
                return 0;

            if (m_Controller == null)
                return baseValue;

            int boostedValue        = m_Caster.StateHandler.ApplyBonusInt(baseValue, property, m_Controller);                                                              // Bonus values applied to the property
            float stacksFactor      = stateEffectScalingStacks.StateEffectProperty == property ? Stacks * stateEffectScalingStacks.ScalingFactor : 1f;                  // apply Stack bonus 

            // return boosted valye
            return (int)Mathf.Round(boostedValue * stacksFactor);    
        }

        public virtual float GetFloat(EStateEffectProperty property) 
        {
            if (TryGetBonusStat(property, out float value))
                return value;

            SStateEffectScaling stateEffectScaling = m_StateEffectScalingStacks.FirstOrDefault(effect => effect.StateEffectProperty == property);

            float baseValue = GetProperty<float>(property);

            if (m_Controller == null)
                return baseValue;

            // if is a slow, check the bonus from the caster bonus slow 
            if (property == EStateEffectProperty.SpeedBonus && baseValue < 0 && m_Caster != null)
            {
                // ADD : && baseValue < 0
                baseValue *= Mathf.Max(0, m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusSlowPerc));
            }

            float boostedValue = m_Controller.StateHandler.ApplyBonus(baseValue, property, null);

            // check that a scaling value was provided
            if (stateEffectScaling.StateEffectProperty != property || stateEffectScaling.ScalingFactor == 0)
                return baseValue;
            // Bonus values applied to the property
            float stacksFactor = stateEffectScaling.StateEffectProperty == property ? Stacks * stateEffectScaling.ScalingFactor : 1f;                  // apply Stack bonus 

            return Mathf.Round(100 * boostedValue * stacksFactor) / 100;
        }

        #endregion


        #region Listeners

        protected virtual void RegisterListeners()
        {
            UnRegisterListeners();

            m_Controller.StateHandler.HoldingStateEffects.OnListChanged += RecheckIsHolding;
        }

        protected virtual void UnRegisterListeners()
        {
            m_Controller.StateHandler.HoldingStateEffects.OnListChanged -= RecheckIsHolding;
        }

        void RecheckIsHolding(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            if (changeEvent.Value != StateEffectName.ToString())
                return;

            m_IsHolding = m_Controller.StateHandler.IsHolding(StateEffectName);
        }

        #endregion


        #region Infos

        public virtual Dictionary<string, object> GetInfos()
        {
            var infosDict = new Dictionary<string, object>();
            if (StateEffectType != EStateEffectType.Default)
                infosDict["Type"] = StateEffectType.ToString();

            foreach (EStateEffectProperty property in Enum.GetValues(typeof(EStateEffectProperty)))
            {
                // check if property exists for this StateEffect and get reflection object
                if (! TryGetPropertyInfo(property, out FieldInfo propertyInfo, false))
                    continue;

                // handles special case properties
                if (GetSpecialPropertiesInfos(ref infosDict, property))
                    continue;

                if (propertyInfo.FieldType == typeof(float))
                { 
                    float value = GetProperty<float>(property);
                    if (value != 0)
                        infosDict.Add(property.ToString(), value);
                }

                else if (propertyInfo.FieldType == typeof(int))
                {
                    int value = GetProperty<int>(property);
                    if (value == 0)
                        continue;
                   
                    infosDict.Add(property.ToString(), value);
                }
            }

            return infosDict;
        }

        /// <summary>
        /// Handle the info management of special cases 
        /// </summary>
        /// <param name="infosDict"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        protected virtual bool GetSpecialPropertiesInfos(ref Dictionary<string, object> infosDict, EStateEffectProperty property)
        {
            switch (property)
            {
                case EStateEffectProperty.None:
                    return true;

                case EStateEffectProperty.MaxStacks:
                    var maxStacks = GetProperty<int>(property);
                    if (maxStacks > 1)
                        infosDict.Add(property.ToString(), maxStacks);
                    return true;

                // don't add stacks
                case EStateEffectProperty.Stacks:
                    return true;

                case EStateEffectProperty.Duration:
                    var duration = GetProperty<float>(property);
                    if (duration > 0)
                        infosDict.Add(property.ToString(), duration);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get Description info of the StateEffect
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescription()
        {
            List<string> values = new List<string>();
            foreach(EStateEffectProperty property in m_DescriptionVariables)
            {
                var value = GetProperty(property);
                if (value == null)
                {
                    values.Add("UNDEFINED");
                    continue;
                }

                string stringValue;
                if (value is float floatValue)
                {
                    stringValue = TextHandler.FormatPropertyValue(floatValue, property.ToString());
                }
                else if (value is double doubleValue)
                {
                    stringValue = doubleValue.ToString("F2");
                }
                else
                {
                    stringValue = value.ToString();
                }

                stringValue += TextHandler.FormatStateEffectIcon(property.ToString(), withIcon: true, withPropertyName: false);

                values.Add(stringValue);
            }

            var description = TextHandler.ReplaceStateEffectTokens(string.Format(m_Description, values.ToArray()));
            description = TextHandler.ReplaceSubStateEffects(description, m_SubStateEffects);
            return description;
        }

        #endregion
    }
}