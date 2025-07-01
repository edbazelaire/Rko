using Assets.Scripts.Data.DataStructures;
using Assets.Scripts.Game;
using Assets.Scripts.Managers.Sound;
using Data;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Character;
using Game.Loaders;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


namespace Game.Spells
{
    [CreateAssetMenu(fileName = "StateEffect", menuName = "Game/StateEffects/Default")]
    [Serializable]
    public class StateEffect : ScriptableObject
    {
        #region Members


        // =========================================================================================
        // ACTIONS
        /// <summary> Event fired at each state effect state </summary>
        public static Action<string, EStateEffectEvent, int, ulong, ulong, string> StateEffectEvent;

        // =========================================================================================
        // SERIALIZED DATA
        [Header("Description")]
        [SerializeField] protected      string                      m_Description = "";
        [SerializeField] protected      List<EStateEffectProperty>  m_DescriptionVariables = new List<EStateEffectProperty>();
        [SerializeField] protected      EStateEffectType            m_StateEffectType = EStateEffectType.Default;

        [Header("Graphics")]
        [SerializeField] protected      bool                        m_IsDisplayed = true;
        [SerializeField] protected      List<SpellPrefabSpawn>      m_VisualEffects;
        [SerializeField] protected      List<StateEffectSpawnGFX>   m_GfxEffects;
        [SerializeField] protected      EAnimation                  m_Animation;
        [SerializeField] protected      AudioClip                   m_OnApplySoundFX;
        [SerializeField] protected      AudioClip                   m_PermanantSoundFX;

        [Header("Consume State")]
        [SerializeField] protected      EStateEffect                m_ConsumeState;
        [SerializeField] protected      EStateEffect                m_DefaultState;

        [Header("General Stats")]
        [SerializeField] protected      bool                        m_IsInstantanious       = false;
        [SerializeField] protected      bool                        m_IsConsumedOnEnd       = false;
        [SerializeField] protected      bool                        m_IsTrueDamage          = false;
        [SerializeField] protected      int                         m_Priority              = 0;

        [Header("Stacks & Duration")]
        [SerializeField] protected      float                       m_Delay                 = 0f;
        [SerializeField] protected      float                       m_Duration              = 0f;
        [SerializeField] protected      int                         m_MaxStacks             = 1;
        [SerializeField, Tooltip("Number of stacks decaying at the end of the duration")]
        protected int                                               m_StackDecay            = -1;

        [Header("Triggers")]
        public bool                                                 m_StartsActivated       = true;
        [SerializeField, Tooltip("List of Triggers linked to their effects")]
        protected List<SStateEffectTrigger>                         m_StateEffectTriggers   = new();
        [SerializeField, Tooltip("List of activations effects that are adding stacks")]
        protected List<SStateEffectActivation>                      m_StateEffectActivations = new();

        [Header("Statistics")]
        [SerializeField] protected      List<SBonusStats>           m_BonusStats;
        [SerializeField] protected      List<SStatConversion>       m_StatConversions       = new();

        [Header("Extra Effects")]
        [SerializeField] protected List<SStateEffectData>           m_SubStateEffects;
        [SerializeField] protected List<string>                     m_HoldingStateEffects;
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
        /// <summary> Spell or Effect that will get credits for damage or heal of this effect </summary>
        protected string                m_Parent;
        /// <summary> Spell or Effect responsable for this effect </summary>
        protected string                m_Origin;
        /// <summary> Level of the state effect </summary>
        protected int                   m_Level;
        /// <summary> Is the effect currently started ? </summary>
        protected bool                  m_IsStarted     = false;
        /// <summary> Is the effect currently active ? </summary>
        protected bool                  m_IsActivated   = true;
        /// <summary> Is the effect currently ending ? </summary>
        protected bool                  m_IsOver        = false;
        /// <summary> Type of state effect </summary>
        protected EStateEffect          m_Type;
        /// <summary> source of audio </summary>
        protected AudioSource           m_AudioSource;

        protected int                   m_Stacks = 1;   
        protected int                   m_RemainingShield;
        protected bool                  m_IsHolding = false;
        protected float                 m_Timer;

        // =========================================================================================
        // DEPENDENT MEMBERS  
        public virtual EStateEffectType StateEffectType     => m_StateEffectType;
        public List<SStateEffectData>   SubStateEffects     => m_SubStateEffects;
        public Controller               Controller          => m_Controller;
        public Controller               Caster              => m_Caster;
        public bool                     IsActivated         => m_IsActivated;
        public bool                     IsDisplayed         => m_IsDisplayed;
        public bool                     IsUnique            => StateEffectType == EStateEffectType.Incarnation;
        public List<StateEffectSpawnGFX> GfxEffects         => m_GfxEffects;
        public List<SpellPrefabSpawn>   VisualEffects       => m_VisualEffects;
        public EAnimation               Animation           => m_Animation;
        public EStateEffect             Type                => Enum.TryParse(name, out EStateEffect type) ? type : m_Type ;
        public virtual int              Stacks              => m_Stacks;
        public virtual int              Priority            => m_Priority;
        public virtual bool             IsInfinite          => ! m_IsInstantanious && m_Duration <= 0;
        public int                      RemainingShield     => m_RemainingShield;
        public bool                     IsInstantanious     => m_IsInstantanious;
        public virtual int              MaxStacks           => m_MaxStacks;
        public EStateEffect             ConsumeState        => m_ConsumeState;
        public EStateEffect             DefaultState        => m_DefaultState;
        public int                      Level               => m_Level;
        public bool                     IsBuff              => SpellLoader.IsSpell(StateEffectName);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="caster"></param>
        /// <param name="stateEffectData"></param>
        /// <returns></returns>
        public virtual bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffectData = null)
        {
            if (controller == null)
            {
                ErrorHandler.Error("Provided Controller is null");
                return false;
            }

            m_Controller    = controller;
            m_Caster        = caster;
            m_IsStarted     = false;                // on init - reset is started 
            m_IsActivated   = false;                // initialize activated to false
            m_IsOver        = false;                // initialize activated to false
            SetStacks(stateEffectData.HasValue ? stateEffectData.Value.GetStacks() : 1);

            // check if has overriding data
            if (stateEffectData.HasValue && stateEffectData.Value.OverridingProperties != null && stateEffectData.Value.OverridingProperties.Count > 0)
                OverrideStateEffectData(stateEffectData.Value.OverridingProperties);

            // allow children to pre-process before starting
            ApplyPreProcessing();

            // check if has condition to be enabled
            if (! CheckBeforeGraphicInit())
            {
                return false;
            }

            // register spell listeners
            RegisterListeners();

            // call state effect that effect has been APPLIED
            CallStateEffectEvent(EStateEffectEvent.OnApplied, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);

            // has delay : activate in "m_Delay" seconds
            if (m_Delay > 0)
            {
                m_Controller.StartCoroutine(ActivateDelay());
                return true;
            }

            // state effect does not starts activated : wait for the Activation to play the "OnStart()" method
            if (! m_StartsActivated)
                return true;

            // activate effects that applies when the spell is ready
            Activate();

            // if spell is instantanious do not proceed after initalization in state handler
            if (m_IsInstantanious)
                return false;

            return true;
        }

        #endregion


        #region At Init 

        /// <summary>
        /// Allow children to run code BEFORE enabling the spell
        /// </summary>
        protected virtual void ApplyPreProcessing() { }

        /// <summary>
        /// Method that allows children to make a verification before instantiating this
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckBeforeGraphicInit()
        {
            if (m_ConsumeState == EStateEffect.None)
                return true;

            SetStacks(Mathf.Min(ApplyConsumeState(m_Stacks), MaxStacks));
            return m_Stacks > 0;
        }

        /// <summary>
        /// Call all method that applies when the spell starts
        /// </summary>
        protected virtual void OnStart()
        {
            // this method can only be called once
            if (m_IsStarted)
                return;

            // set that the method has been called
            m_IsStarted = true;

            // reset data before start
            RefreshStats();

            // start holding
            ActivateHoldingStateEffects(true);

            // apply cooldown reduction effects of that state effect
            ApplyCooldownReduction();

            ApplySubStateEffect();

            ApplyOnStartStateEffects();

            // if instantatious effect : end after start
            if (m_IsInstantanious)
            {
                End();
                return;
            }

            // is that type of effect currently "Holding" (= duration timer is frozen)
            m_IsHolding = m_Controller.StateHandler.IsHolding(StateEffectName);

            // allow children to call post-processing methods
            ApplyPostProcessing();

            // is that type of effect currently "Holding" (= duration timer is frozen)
            m_IsHolding = m_Controller.StateHandler.IsHolding(StateEffectName);

            // allow children to call post-processing methods
            ApplyPostProcessing();
        }

        /// <summary>
        /// Allow children to run code AFTER enabling the spell
        /// </summary>
        protected virtual void ApplyPostProcessing() { }

        /// <summary>
        /// Allow wraper data to override the state effect's data
        /// </summary>
        public void OverrideStateEffectData(List<SStateEffectProperty> overridingProperties)
        {
            if (overridingProperties == null)
                return;

            foreach (SStateEffectProperty overridingProperty in overridingProperties)
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

        public virtual int ForceEnd(bool consume)
        {   
            // check is already ending
            if (m_IsOver)
                return 0;

            // call that this is ending
            m_IsOver = true;

            // call consume if required
            if (consume)
                CallStateEffectEvent(EStateEffectEvent.OnConsumed, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);

            // call end
            CallStateEffectEvent(EStateEffectEvent.OnEnd, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);

            // clean sub effects
            RemoveSubStateEffects();
            ActivateHoldingStateEffects(false);

            // destroy the object
            Destroy(this);

            // return current number of stacks
            return m_Stacks;
        }

        /// <summary>
        /// Reached its end naturaly or was consumed by another spell
        /// </summary>
        public virtual void End()
        {
            if (! IsInstantanious)
                m_Controller.StateHandler.RemoveFromList(StateEffectName);

            ForceEnd(m_IsConsumedOnEnd);
        }

        protected virtual void OnDestroy()
        {
            if (m_AudioSource != null)
                Destroy(m_AudioSource);

            if (m_Controller == null)
                return;

            UnRegisterListeners();
        }

        #endregion


        #region Activation / Deactivation

        public virtual void Activate()
        {
            if (m_IsActivated)
                return;

            if (! m_IsStarted)
                OnStart();

            m_IsActivated = true;
            CallStateEffectEvent(EStateEffectEvent.OnActivated, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);
        }

        public virtual void Deactivate()
        {
            if (! m_IsActivated)
                return;

            m_IsActivated = false;
            CallStateEffectEvent(EStateEffectEvent.OnDeactivated, m_Stacks, m_Controller.PlayerId, m_Caster.PlayerId);
        }

        public virtual IEnumerator ActivateDelay()
        {
            m_IsActivated = false;
            if (m_Delay > 0)
                yield return new WaitForSeconds(m_Delay);

            Activate();
        }

        #endregion


        #region Update & Refresh

        public virtual void Update()
        {
            if (IsInfinite)
                return;

            if (m_IsHolding || !m_IsActivated || !m_IsStarted)
                return;

            m_Timer -= Time.deltaTime;

            if (m_Timer <= 0)
            {
                // refresh timer
                m_Timer = m_Duration;

                // remove N stacks
                if (m_StackDecay < 0)
                {
                    RemoveStacks(Math.Abs(m_StackDecay), consume: false);
                }
                else
                {
                    Refresh(m_StackDecay);
                }
            }
        }

        /// <summary>
        /// Refresh an effect and add N stacks. Also upgrade level if the spell that is refreshing the spell is higher level
        /// </summary>
        /// <param name="stacks"></param>
        /// <param name="level"></param>
        public virtual void Refresh(int stacks = 0, int level = 0)
        {
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

            // re-adjust level
            if (m_Stacks > 0 && level > 0)
            {
                SetLevel((int)Math.Round((float)(m_Level * m_Stacks + level * stacks) / (m_Stacks + stacks)));
            }

            SetStacks(m_Stacks + stacks);
            RefreshStats();

            CallStateEffectEvent(EStateEffectEvent.OnRefreshed, stacks, m_Controller.PlayerId, m_Caster.PlayerId);
        }

        protected virtual void SetStacks(int stacks)
        {
            if (m_MaxStacks > 0)
                m_Stacks = Math.Clamp(stacks, 0, m_MaxStacks);
            else
                m_Stacks = Math.Max(stacks, 0);
        }

        protected virtual void RefreshStats()
        {
            var currentShield = m_RemainingShield;
            m_RemainingShield = m_Controller.StateHandler.ApplyBonusShield(GetInt(EStateEffectProperty.Shield), m_Controller, specialCondition: StateEffectName); 
            m_Timer = m_Duration;

            var shieldAdded = m_RemainingShield - currentShield;
            if (shieldAdded > 0)
                GameAnalyticsManager.Instance.OnSpellHit(m_Controller.PlayerId, m_Controller.PlayerId, StateEffectName, shieldAdded, EHitType.Shield, ESpellCategory.Direct);
        }

        public virtual int RemoveStacks(int nStacks, bool consume = false)
        {
            // adjust nStacks to correct value (not over current number of stacks)
            nStacks = nStacks >= m_Stacks || nStacks < 0 ? m_Stacks : nStacks;

            // call StateEffect EVENT 
            CallStateEffectEvent(
                consume ? EStateEffectEvent.OnConsumed : EStateEffectEvent.OnRemoved,
                nStacks,
                m_Controller.PlayerId,
                m_Caster.PlayerId
            );

            // if not enough stacks - end the state effect
            if (nStacks >= m_Stacks || nStacks <= 0)
            {
                End();
                return nStacks;
            }

            // update stacks
            SetStacks(m_Stacks - nStacks);

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
                m_Controller.StateHandler.AddStateEffect(m_DefaultState.ToString(), m_Caster, m_Level, m_Origin);

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

        protected virtual void ApplyCooldownReduction()
        {
            if (! TryGetBonusStatValue(EStateEffectProperty.CooldownReduction, out float cooldownReduction, stacks: Stacks))
                return;

            m_Controller.SpellHandler.ReduceCooldowns(cooldownReduction);
        }

        protected virtual void ApplySubStateEffect()
        {
            if (m_SubStateEffects == null)
                return;
            
            foreach (var subStateEffect in m_SubStateEffects)
            {
                m_Controller.StateHandler.AddStateEffect(subStateEffect, m_Controller, level: m_Level, origin: m_Origin);
            }
        }

        protected virtual void RemoveSubStateEffects()
        {
            if (m_SubStateEffects == null)
                return;

            foreach (var subStateEffect in m_SubStateEffects)
            {
                // get stateEffectData overwritten with provided data
                var stateEffectData = SpellLoader.GetStateEffect(subStateEffect.StateEffect.ToString());
                stateEffectData.OverrideStateEffectData(subStateEffect.OverridingProperties);
                
                // check if is Inifinite (meaning the sub StateEffect end is linked to the end of this effect)
                if (stateEffectData.IsInfinite)
                    m_Controller.StateHandler.RemoveStateEffect(subStateEffect.StateEffect);
            }
        }

        protected virtual void ApplyOnStartStateEffects()
        {
            if (m_OnStartStateEffect == null)
                return;

            foreach (var stateEffect in m_OnStartStateEffect)
            {
                var clone = stateEffect.Clone(m_Level, parent: m_Parent, origin: m_Origin);
                clone.m_Duration = m_Duration;                      // duplicate duration
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
            // no shield - skip
            if (m_RemainingShield <= 0)
                return damages;

            // not activated - skip
            if (! m_IsActivated)
                return damages;

            m_RemainingShield -= damages;
            if (m_RemainingShield >= 0)
                return 0;

            damages = -m_RemainingShield;
            m_RemainingShield = 0;
            return damages;
        }

        #endregion


        #region Level Scaling Methods

        public virtual StateEffect Clone(int level = 0, string parent = "", string origin = "")
        {
            StateEffect clone = Instantiate(this);
            clone.name = this.name;

            // make sure that original level is copied
            clone.m_Level = m_Level;
            clone.SetLevel(level <= 0 ? m_Level : level);
            clone.SetParent(parent ?? m_Parent);            
            clone.SetOrigin(origin ?? m_Origin);     // duplicate origin

            return clone;
        }

        protected virtual void SetLevel(int level)
        {
            ApplyNewLevelFactorAll(level);
            m_Level = level;

            if (m_SubStateEffects != null)
            {
                for (int i = 0; i < m_SubStateEffects.Count; i++)
                {
                    var stateEffect = m_SubStateEffects[i];
                    stateEffect.SetLevel(level);
                    m_SubStateEffects[i] = stateEffect;
                }
            }
            
        }

        public virtual void SetParent(string parent)
        {
            if (parent.IsNullOrEmpty())
            {
                m_Parent = StateEffectName;
                return;
            }

            m_Parent = parent;
        }

        public virtual void SetOrigin(string spellOrigin)
        {
            m_Origin = spellOrigin;
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

            if (! m_StatConversions.IsNullOrEmpty() && m_StatConversions.Any(value => value.HasStat(property)))
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
            // CHECK : Bonus Stats
            if (TryGetBonusStat(property, out SBonusStats bonusStats))
            {
                if (! float.TryParse(value.ToString(), out float fValue))
                {
                    ErrorHandler.Error($"{StateEffectName} - trying to override property {property} with value {value} but the value cant be parse into a float");
                    return;
                }

                Debug.LogWarning($"{StateEffectName} - Setting property {property} with value {value}");
                bonusStats.BaseValue = fValue;
                return;
            }

            // If the value does not exist "by default" - add in bonus data
            if (!TryGetPropertyInfo(property, out FieldInfo propertyInfo, throwError: false))
            {
                if (float.TryParse(value.ToString(), out float fValue))
                {
                    if (m_BonusStats == null)
                        m_BonusStats = new();

                    Debug.LogWarning($"{StateEffectName} - Adding property {property} with value {value}");
                    m_BonusStats.Add(new SBonusStats(property, fValue));
                    return;
                }

                ErrorHandler.Warning("Unable to parse " + property + " with value " + value + " into a float");
                return;
            }

            // ================================================================================
            // [DEPRECATED] - old method

            // INT
            if (propertyInfo.FieldType == typeof(int))
            {
                if (int.TryParse(value.ToString(), out int convertedValue))
                    propertyInfo.SetValue(this, convertedValue);
                else
                    ErrorHandler.Error("Unable to set value (" + value + ") of " + property + " as int");
                return;
            }

            // FLOAT
            else if (propertyInfo.FieldType == typeof(float))
            {
                if (float.TryParse(value.ToString(), out float convertedValue))
                    propertyInfo.SetValue(this, convertedValue);
                else
                    ErrorHandler.Error("Unable to set value (" + value + ") of " + property + " as int");
                return;
            }

            // default
            propertyInfo.SetValue(this, value);
            // ================================================================================
        }

        /// <summary>
        /// Get the value of a property by Reflection
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual object GetProperty(EStateEffectProperty property, bool ignoreConversion = false, string specialCondition = "")
        {
            object value;

            // check BONUS stats
            if (TryGetBonusStatValue(property, out float fValue, Stacks, specialCondition: specialCondition))
                value = fValue;

            // [DEPRECATED] check PROPERTY info
            else if (TryGetPropertyInfo(property, out FieldInfo propertyInfo, throwError: false))
                value = propertyInfo.GetValue(this);

            // set to 0 by default
            else
                value = 0f;

            // add bonus STATS CONVERSIONS
            if (!ignoreConversion && ! m_StatConversions.IsNullOrEmpty())
            {
                foreach (SStatConversion statConversion in m_StatConversions)
                {
                    if (statConversion.HasStat(property))
                    {
                        if (value == null)
                            value = 0f;

                        if (! float.TryParse(value.ToString(), out fValue))
                        {
                            ErrorHandler.Error("Unable to parse property " + property + "(" + value + ") into a float");
                            break;
                        }

                        value = fValue + statConversion.Get(m_Controller, Level, Stacks);
                    }
                }
            }

            return value;
        }

        protected virtual T GetProperty<T>(EStateEffectProperty property, bool ignoreConversion = false)
        {
            object value = GetProperty(property, ignoreConversion);
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

        public virtual bool TryGetBonusStat(EStateEffectProperty property, out SBonusStats bonusStats, string specialCondition = "")
        {
            // init value
            bonusStats = default;

            // check if bonus stats provided
            if (m_BonusStats.IsNullOrEmpty())
                return false;

            // check that bonus stats has requested value
            if (! TryGetBonusStatIndex(property, out int index, specialCondition))
                return false;

            // get bonus stats
            bonusStats = m_BonusStats[index];
            return true;
        }

        public virtual bool TryGetBonusStatIndex(EStateEffectProperty property, out int index, string specialCondition = "")
        {
            // init value
            index = -1;

            // check if bonus stats provided
            if (m_BonusStats.IsNullOrEmpty())
                return false;

            index = m_BonusStats.FirstIndex(value => value.StateEffectProperty == property && value.HasSpecialCondition(specialCondition));
            if (index < 0)
                return false;

            return true;
        }

        public virtual bool TryGetBonusStatValue(EStateEffectProperty property, out float value, int stacks, string specialCondition = "")
        {
            value = 0f;

            // check if bonus stats exists
            if (! TryGetBonusStat(property, out SBonusStats bonusStats, specialCondition))
                return false;

            value = bonusStats.Get(m_Level, stacks, specialCondition);
            return true;
        }

        public virtual int GetInt(EStateEffectProperty property, int? stacks = null, string specialCondition = "") 
        {
            if (TryGetBonusStatValue(property, out float value, stacks ?? Stacks, specialCondition))
                return (int)Mathf.Round(value);

            if (!HasEffectProperty(property))
                return 0;

            SStateEffectScaling stateEffectScalingStacks = m_StateEffectScalingStacks.FirstOrDefault(effect => effect.StateEffectProperty == property);

            int baseValue = GetProperty<int>(property);                                             // Base Value of the property

            if (baseValue == 0)
                return 0;

            if (m_Controller == null)
                return baseValue;

            int boostedValue        = m_Caster.StateHandler.ApplyBonusInt(baseValue, property, m_Controller, specialCondition);                                                              // Bonus values applied to the property

            // get percentage 
            stacks ??= Stacks;
            float stacksFactor      = stateEffectScalingStacks.StateEffectProperty == property ? Stacks * stateEffectScalingStacks.ScalingFactor : 1f;                  // apply Stack bonus 

            // return boosted valye
            return (int)Mathf.Round(boostedValue * stacksFactor);    
        }

        public virtual float GetFloat(EStateEffectProperty property, bool ignoreConversion = false, int? stacks = null, string specialCondition = "") 
        {
            if (TryGetBonusStatValue(property, out float value, stacks ?? Stacks, specialCondition))
                return value;

            if (!HasEffectProperty(property))
                return 0f;

            SStateEffectScaling stateEffectScaling = m_StateEffectScalingStacks.FirstOrDefault(effect => effect.StateEffectProperty == property);

            float baseValue = GetProperty<float>(property, ignoreConversion);

            if (m_Controller == null)
                return baseValue;

            // if is a slow, check the bonus from the caster bonus slow 
            if (property == EStateEffectProperty.SpeedBonus && baseValue < 0 && m_Caster != null)
            {
                // ADD : && baseValue < 0
                baseValue *= Mathf.Max(0, m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusSlowPerc, ignoreConversion: ignoreConversion));
            }

            float boostedValue = m_Controller.StateHandler.ApplyBonus(baseValue, property, null, specialCondition: StateEffectName);

            // check that a scaling value was provided
            if (stateEffectScaling.StateEffectProperty != property || stateEffectScaling.ScalingFactor == 0)
                return baseValue;
            // Bonus values applied to the property
            stacks ??= Stacks;
            float stacksFactor = stateEffectScaling.StateEffectProperty == property && stateEffectScaling.ScalingFactor != 0 ? stacks.Value * stateEffectScaling.ScalingFactor : 1f;                  // apply Stack bonus 

            return Mathf.Round(100 * boostedValue * stacksFactor) / 100;
        }

        #endregion


        #region State Effect Events

        protected virtual void CallStateEffectEvent(EStateEffectEvent stateEffectEvent, int stacks, ulong targetId, ulong casterId)
        {
            ErrorHandler.Log($"{StateEffectName} - {stateEffectEvent} : {stacks} stacks", ELogTag.StateEffects);
            
            // call event on server side
            StateEffect.StateEffectEvent?.Invoke(StateEffectName, stateEffectEvent, stacks, targetId, casterId, m_Origin);

            switch (stateEffectEvent)
            {
                case EStateEffectEvent.OnApplied:
                    OnApplied(stacks);
                    break;

                case EStateEffectEvent.OnRefreshed:
                    OnRefreshed(stacks);
                    break;

                case EStateEffectEvent.OnTick:
                    OnTick();
                    break;

                case EStateEffectEvent.OnRemoved:
                    OnRemoved(stacks); 
                    break;

                case EStateEffectEvent.OnConsumed:
                    OnConsumed(stacks); 
                    break;

                case EStateEffectEvent.OnActivated:
                    OnActivated(); 
                    break;

                case EStateEffectEvent.OnDeactivated:
                    OnDeactivated(); 
                    break;

                case EStateEffectEvent.OnEnd:
                    OnEnd(); 
                    break;
            }

            // CLIENT RPC  ---------------------------------------------------------------------
            if (HasClientEventAt(stateEffectEvent))
            {
                m_Controller.StateHandler.CallStateEffectEventClientRPC(new StateEventData(stateEffectEvent, StateEffectName, casterId, stacks, m_MaxStacks, m_Duration));
            }
        }

        protected virtual void OnApplied(int stacks) 
        {
            OnRefreshed(stacks);
        }

        protected virtual void OnRefreshed(int stacks) 
        {
            if (stacks == 0)
                return;

            // ================================================================================================
            // ENERGY                   - check if should add energy
            int value = GetInt(EStateEffectProperty.Energy, stacks);
            if (value != 0)
                m_Caster.EnergyHandler.AddEnergy(value);

            // ================================================================================================
            // DAMAGE                   -  check if should damage the target
            value = GetInt(EStateEffectProperty.Damage, stacks);
            if (value != 0)
            {
                // add bonus damage IF there are special "bonus damage" for this effect (StateEffect dont use common bonus damage)
                value = m_Caster.StateHandler.ApplyBonusDamage(value, m_Controller, specialCondition: SBonusStats.AsUnique(StateEffectName));

                // hit target
                m_Controller.Life.Hit(value, casterId: m_Caster.PlayerId, source: m_Parent, spellCategory: ESpellCategory.Direct, ignoreRes: m_IsTrueDamage);

                // apply lifesteal (on caster)
                var lifesteal = Mathf.Max(0f, GetInt(EStateEffectProperty.LifeSteal) + m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal, m_Controller, specialCondition: SBonusStats.AsUnique(StateEffectName)) - 1);
                if (lifesteal > 0)
                {
                    m_Caster.Life.Heal((int)Mathf.Round(value * lifesteal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);
                }
            }

            // ================================================================================================
            // EXECUTION DAMAGE         -  check if should apply execution damage on the target
            value = GetInt(EStateEffectProperty.ExecutionDamage, stacks);
            if (value != 0)
            {
                // add bonus execution damage
                value = m_Caster.StateHandler.ApplyBonusExecutionDamage(value, m_Controller, specialCondition: StateEffectName);

                // hit target
                m_Controller.Life.Hit(value, casterId: m_Caster.PlayerId, source: m_Parent, spellCategory: ESpellCategory.Direct, ignoreRes: m_IsTrueDamage);

                // apply lifesteal (on caster)
                var lifesteal = Mathf.Max(0f, GetInt(EStateEffectProperty.LifeSteal) + m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal, m_Controller, specialCondition: SBonusStats.AsUnique(StateEffectName)) - 1);
                if (lifesteal > 0)
                {
                    m_Caster.Life.Heal((int)Mathf.Round(value * lifesteal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);
                }
            }

            // ================================================================================================
            // HEALING                  -  check if should heal the target
            value = GetInt(EStateEffectProperty.Heal, stacks);
            if (value != 0)
                m_Controller.Life.Heal(value, casterId: m_Caster.PlayerId, source: m_Parent, spellCategory: ESpellCategory.Direct);

            // ================================================================================================
            // SHIELD                   -  check if should shield the target
            value = GetInt(EStateEffectProperty.Shield, stacks);
            if (value != 0)
                m_RemainingShield += value;
        }

        protected virtual void OnTick() { }

        protected virtual void OnRemoved(int stacks) { }

        protected virtual void OnConsumed(int stacks) 
        {
            int value;

            // check if should add energy
            value = GetInt(EStateEffectProperty.EndDamage, stacks);
            if (value != 0)
            {
                // hit target
                m_Controller.Life.Hit(value, casterId: m_Caster.PlayerId, source: StateEffectName, spellCategory: ESpellCategory.Direct, ignoreRes: m_IsTrueDamage);

                // apply lifesteal (on caster)
                var lifesteal = Mathf.Max(0f, GetInt(EStateEffectProperty.LifeSteal) + m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal, m_Controller) - 1);
                if (lifesteal > 0)
                {
                    m_Caster.Life.Heal((int)Mathf.Round(value * lifesteal), m_Caster.PlayerId, StateEffectName, ESpellCategory.Direct);
                }
            }

            // check if should add energy
            value = GetInt(EStateEffectProperty.EndHeal, stacks);
            if (value != 0)
                m_Controller.Life.Heal(value, casterId: m_Caster.PlayerId, source: StateEffectName, spellCategory: ESpellCategory.Direct);
        }

        protected virtual void OnActivated() { }

        protected virtual void OnDeactivated() { }

        protected virtual void OnEnd() { }

        #endregion


        #region Client Event

        bool HasClientEventAt(EStateEffectEvent stateEffectEvent)
        {
            // TODO ! 
            return true;
        }

        #endregion


        #region Listeners

        protected virtual void RegisterListeners()
        {
            m_Controller.StateHandler.HoldingStateEffects.OnListChanged += RecheckIsHolding;
            RegisterTriggers();
        }

        protected virtual void UnRegisterListeners()
        {
            m_Controller.StateHandler.HoldingStateEffects.OnListChanged -= RecheckIsHolding;
            UnRegisterTriggers();
        }

        void RecheckIsHolding(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            if (changeEvent.Value != StateEffectName.ToString())
                return;

            m_IsHolding = m_Controller.StateHandler.IsHolding(StateEffectName);
        }

        void RegisterTriggers()
        {
            foreach (var trigger in m_StateEffectTriggers)
            {
                trigger.Register(Controller, this);
            }
        }

        void UnRegisterTriggers()
        {
            foreach (var trigger in m_StateEffectTriggers)
            {
                trigger.UnRegister();
            }
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
                if (property == EStateEffectProperty.Level)
                    continue;

                if (!HasEffectProperty(property))
                    continue;

                // handles special case properties
                if (GetSpecialPropertiesInfos(ref infosDict, property))
                    continue;

                var value = GetProperty(property, ignoreConversion: true);
                if (value is float fValue && fValue == 0f)
                    continue;
                if (value is int iValue && iValue == 0)
                    continue;

                infosDict.Add(property.ToString(), value);
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

                case EStateEffectProperty.ConsumeState:
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
                var value = GetProperty(property, true);
                if (value == null)
                {
                    values.Add($"<color={Color.magenta.ToHex()}>UNDEFINED</color>");
                    continue;
                }

                // check if effect is scaling
                IsScalingProperty(property, out EScalingDirection scaling);

                // add to the list of replacing data
                values.Add(TextHandler.FormatPropertyIcon(property.ToString(), value, withPropertyName: false, scaling: scaling));
            }

            var description = TextHandler.ReplaceStateEffectTokens(string.Format(m_Description, values.ToArray()));
            description = TextHandler.ReplaceStateEffectProperties(description, this);
            ReplaceSubStateEffects(ref description);
            return description;
        }

        protected virtual void ReplaceSubStateEffects(ref string description)
        {
            description = TextHandler.ReplaceSubStateEffects(description, m_SubStateEffects);
        }

        /// <summary>
        /// Check if provided property is scaling or not
        /// </summary>
        /// <returns></returns>
        public virtual bool IsScalingProperty(string property, out EScalingDirection scaling)
        {
            scaling = EScalingDirection.None;
            if (!Enum.TryParse(property, true, out EStateEffectProperty stateEffectProperty))
                return false;

            return IsScalingProperty(stateEffectProperty, out scaling);
        }

        public virtual bool IsScalingProperty(EStateEffectProperty property, out EScalingDirection scaling)
        {
            scaling = EScalingDirection.None;

            // check if bonus stats provided
            if (m_BonusStats.IsNullOrEmpty() || ! m_BonusStats.Any(value => value.StateEffectProperty == property))
            {
                // [DEPRECATED] otherwise : check old scaling method 
                if (!m_StateEffectScalingLevel.Any(value => value.StateEffectProperty == property))
                    return default;

                var stateEffectScaling = m_StateEffectScalingLevel.FirstOrDefault(value => value.StateEffectProperty == property);
                if (stateEffectScaling.ScalingFactor > 0)
                {
                    scaling = EScalingDirection.Up;
                    return true;
                }

                if (stateEffectScaling.ScalingFactor < 0)
                {
                    scaling = EScalingDirection.Down;
                    return true;
                }
            }

            // get bonus stats
            SBonusStats bonusStats = m_BonusStats.FirstOrDefault(value => value.StateEffectProperty == property);
            if (bonusStats.LevelScalingFactor > 0)
            {
                scaling = EScalingDirection.Up;
                return true;
            }

            if (bonusStats.LevelScalingFactor < 0)
            {
                scaling = EScalingDirection.Down;
                return true;
            }

            return false;
        }

        #endregion
    }
}