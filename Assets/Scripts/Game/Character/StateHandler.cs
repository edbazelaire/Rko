using Data;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Loaders;
using Game.Spells;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class StateHandler : NetworkBehaviour
    {
        #region Members

        // ==============================================================================================
        // EVENTS
        // used to signal client GFX about spell events
        public event Action<EStateEffectEvent, string, int, int, float> StateEffectEvent;
        public event Action<string, int>                                QuestThresholdEvent;

        // ==============================================================================================
        // PRIVATE ACCESSORS
        // -- Network Variables
        MNetworkList<FixedString64Bytes>    m_HoldingStateEffects;
        NetworkVariable<float>              m_SpeedBonus = new(0f);
        NetworkVariable<EAnimation>         m_AnimationState = new(EAnimation.None);

        // -- SERVER SIDE
        Controller              m_Controller;
        CharacterData           m_CharacterData;
        List<StateEffect>       m_StateEffects;
        int                     m_RemainingShield = 0;

        // ==============================================================================================
        // PUBLIC ACCESSORS
        public CharacterData CharacterData => m_CharacterData;
        public List<StateEffect> StateEffects => m_StateEffects;
        public NetworkList<FixedString64Bytes> HoldingStateEffects => m_HoldingStateEffects;
        public bool IsStunned => 
            ! IsUncontrollable
            && (
                HasState(EStateEffect.Stun.ToString()) 
                || HasState(EStateEffect.Scorched.ToString())
            );

        public bool IsAirborned => 
            ! IsUncontrollable
            && (HasState(EStateEffect.Airborne.ToString())
            );

        public bool IsSilenced =>
            HasState(EStateEffect.Silence.ToString()) 
            || HasState(EStateEffect.Malediction.ToString());

        public bool IsTaunting => HasState(EStateEffect.Taunt);

        public bool CanCast =>
            ! IsSilenced 
            && ! IsStunned 
            && ! IsAirborned 
            && ! HasState(EStateEffect.Frozen)
            && ! HasState(EStateEffect.Jump)
            && ! HasState(EStateEffect.BlockCast);

        public bool CanMove =>
            ! IsStunned 
            && ! IsAirborned 
            && ! HasState(EStateEffect.Frozen)
            && ! HasState(EStateEffect.Jump)
            && ! HasState(EStateEffect.BlockMovement)
            && ! HasState(EStateEffect.SpecialAnimation);

        public bool CanGainEnergy => !HasState(EStateEffect.BlockEnergyGain);


        public bool IsInvulnerable => 
            HasState(EStateEffect.Invulnerable.ToString())
            || HasState(EStateEffect.Jump.ToString())
            || HasState(EStateEffect.Vanish.ToString())
            || HasState(EStateEffect.SpecialAnimation.ToString());

        public bool IsUncontrollable => 
            HasState(EStateEffect.Uncontrollable.ToString())
            || HasState(EStateEffect.Vanish.ToString())
            || HasState(EStateEffect.SpecialAnimation.ToString());

        public bool IsUnTargetable => 
            HasState(EStateEffect.UnTargettable.ToString())
            || HasState(EStateEffect.SpecialAnimation.ToString())
            || HasState(EStateEffect.Vanish.ToString())
            || HasState(EStateEffect.Jump.ToString())
            || HasState(EStateEffect.Invisible.ToString());

        public bool IsImmunedToEffects =>
            HasState(EStateEffect.SpecialAnimation)
            || HasState(EStateEffect.Vanish.ToString());

        public bool IsImmunedToSlows =>
            HasState(EStateEffect.SpecialAnimation)
            || HasState(EStateEffect.Unstoppable.ToString());


        public NetworkVariable<float> SpeedBonus            => m_SpeedBonus;
        public int RemainingShield                          => m_RemainingShield;
        public NetworkVariable<EAnimation> AnimationState   => m_AnimationState;

        #endregion


        #region Init & End

        private void Awake()
        {
            // init network lists
            m_HoldingStateEffects   = new MNetworkList<FixedString64Bytes>();

            // init components 
            m_Controller = GetComponent<Controller>();
            m_StateEffects = new List<StateEffect>();   
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public void Initialize(CharacterData characterData)
        {
            m_CharacterData = characterData;
        }


        #endregion


        #region Inherited Manipulators

        void Update()
        {
            // only server applies the effects
            if (! IsServer || !m_Controller.Life.IsAlive) 
                return;

            for (int i = m_StateEffects.Count - 1; i >= 0; i--)
            {
                if (i >= m_StateEffects.Count)
                {
                    ErrorHandler.Warning("Bad index (" + i + ") for " + gameObject.name);
                    continue;
                }

                m_StateEffects[i].Update();

                if (!m_Controller.Life.IsAlive)
                    return;
            }
        }

        #endregion


        #region GFX Events

        [ClientRpc]
        public void CallStateEffectEventClientRPC(StateEventData stateEventData)
        {
            // Call SpellGFX event
            CallOnStateEffectEventUI(stateEventData.StateEffectEvent, stateEventData.StateEffectName.ToString(), stateEventData.Stacks, stateEventData.MaxStacks, stateEventData.Duration, stateEventData.CasterId);
        }

        void CallOnStateEffectEventUI(EStateEffectEvent stateEffectEvent, string stateEffectName, int stacks, int maxStacks, float duration, ulong casterId)
        {
            ErrorHandler.Log(stateEffectName + " " + stateEffectEvent, ELogTag.StateEffectGFX);

            // event already called on SERVER side
            StateEffectEvent?.Invoke(stateEffectEvent, stateEffectName, stacks, maxStacks, duration);

            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectName);
            if (stateEffect.GfxEffects is null)
                return;

            foreach (var spawnPrefab in stateEffect.GfxEffects)
            {
                if (spawnPrefab.GFXLifetime.StartSpellPart != stateEffectEvent)
                    continue;

                spawnPrefab.Spawn(GameManager.Instance.GetPlayer(casterId), null, null, stateEffectName, m_Controller, transform.position);
            }
        }

        [ClientRpc]
        public void CallQuestThresholdEventClientRPC(string stateEffectName, int index)
        {
            // Call SpellGFX event
            QuestThresholdEvent?.Invoke(stateEffectName, index);
        }

        #endregion


        #region Public Accessors

        public bool HasState(string stateEffectName, bool checkActivated = true)
        {
            if (GameManager.IsGameOver)
                return false;
            
            return m_StateEffects.FindIndex(stateEffect => stateEffect.StateEffectName == stateEffectName && (!checkActivated || stateEffect.IsActivated)) != -1;
        }

        public bool HasState(EStateEffect state, bool checkActivated = true)
        {
            return HasState(state.ToString(), checkActivated);
        }

        public void SetStateJump(bool on)
        {
            if (!IsServer)
                return;

            // diseable collider
            m_Controller.Collider.enabled = !on;

            if (on)
                AddStateEffect(new SStateEffectData(EStateEffect.Jump, overridingProperties: new List<SStateEffectProperty> { new SStateEffectProperty(EStateEffectProperty.Duration, -1f) }), m_Controller, 1, "");
            else
                RemoveStateEffect(EStateEffect.Jump);
        }

        public int ApplyResistance(int damage, ESpellCategory spellCategory = ESpellCategory.Direct)
        {
            // TICK
            if (spellCategory == ESpellCategory.Tick || spellCategory == ESpellCategory.Zone)
                return ApplyTickResistance(damage);

            // apply res fix first
            damage = Math.Max(0, damage - GetInt(EStateEffectProperty.ResistanceFix));

            // apply percentage res
            damage = (int)Mathf.Round(damage * Mathf.Max(2 - GetFloat(EStateEffectProperty.ResistancePerc), 0));
            
            return damage;
        }

        public int ApplyTickResistance(int damage)
        {
            // apply res fix first
            damage = Math.Max(0, damage - GetInt(EStateEffectProperty.ResistanceTick));

            // apply percentage res
            damage = (int)Mathf.Round(damage * Mathf.Max(2 - GetFloat(EStateEffectProperty.ResistanceTickPerc), 0));

            return damage;
        }

        public int ApplyBonusHealReceived(int heal)
        {
            // apply fix heal reduction
            heal = Math.Max(0, heal + GetInt(EStateEffectProperty.HealReduction));

            // apply percentage reduction
            return (int)Mathf.Round(heal * Mathf.Max(2 - GetFloat(EStateEffectProperty.HealReductionPerc), 0));
        }

        public int ApplyBonusDamage(int damage, Controller targetController, string specialCondition = "")
        {
            ErrorHandler.Log("Base Damage : " + damage, ELogTag.BonusStats);

            // apply fix bonus damages 
            damage = Math.Max(0, damage + GetInt(EStateEffectProperty.BonusDamage, targetController, specialCondition));

            ErrorHandler.Log("Damage + Fix : " + damage, ELogTag.BonusStats);

            // apply res fix first
            damage = Math.Max(0, (int)Mathf.Round(damage * GetFloat(EStateEffectProperty.BonusDamagePerc, targetController, specialCondition: specialCondition)));

            ErrorHandler.Log("Final : " + damage, ELogTag.BonusStats);

            return damage;
        }

        public int ApplyBonusTickDamage(int damage, Controller targetController, string specialCondition = "")
        {
            ErrorHandler.Log("Base Damage : " + damage, ELogTag.BonusStats);

            // apply fix bonus damages 
            damage = Math.Max(0, damage + GetInt(EStateEffectProperty.BonusTickDamage, targetController, specialCondition));
            if (specialCondition != "")
                damage = Math.Max(0, damage + GetInt(EStateEffectProperty.BonusDamage, targetController, SBonusStats.AsUnique(specialCondition)));

            ErrorHandler.Log("Damage + Fix : " + damage, ELogTag.BonusStats);

            // apply res fix first
            damage = Math.Max(0, (int)Mathf.Round(damage * GetFloat(EStateEffectProperty.BonusTickDamagePerc, targetController, specialCondition: specialCondition)));
            if (specialCondition != "")
                damage = Math.Max(0, damage + GetInt(EStateEffectProperty.BonusDamagePerc, targetController, SBonusStats.AsUnique(specialCondition)));

            ErrorHandler.Log("Final : " + damage, ELogTag.BonusStats);

            return damage;
        }

        public int ApplyBonusExecutionDamage(int damage, Controller targetController, string specialCondition = "")
        {
            ErrorHandler.Log("Base Execution Damage : " + damage, ELogTag.BonusStats);

            // apply fix bonus damages 
            damage = Math.Max(0, damage + GetInt(EStateEffectProperty.BonusExecutionDamage, targetController) + GetInt(EStateEffectProperty.BonusDamage, targetController, specialCondition));

            ErrorHandler.Log("Damage + Fix : " + damage, ELogTag.BonusStats);

            // apply res fix first
            damage = Math.Max(0, (int)Mathf.Round(damage * GetFloat(EStateEffectProperty.BonusExecutionDamagePerc, targetController, specialCondition: specialCondition) * GetFloat(EStateEffectProperty.BonusDamagePerc, targetController)));

            ErrorHandler.Log("Final : " + damage, ELogTag.BonusStats);

            return damage;
        }

        public int ApplyBonusHealDealt(int heal, Controller targetController, string specialCondition = "")
        {
            // apply percentage res
            heal = Math.Max(0, heal + GetInt(EStateEffectProperty.BonusHeal));

            // apply res fix first
            return Math.Max(0, (int)Mathf.Round(heal * GetFloat(EStateEffectProperty.BonusHealPerc, targetController, specialCondition: specialCondition)));   
        }

        public int ApplyBonusShield(int shield, Controller targetController, string specialCondition = "")
        {
            return Math.Max(0, (int)Mathf.Round(shield * GetFloat(EStateEffectProperty.BonusShieldPerc, targetController, specialCondition: specialCondition)));
        }

        public float ApplyBonus(float baseValue, EStateEffectProperty stateEffectProperty, Controller targetController, string specialCondition = "")
        {
            switch (stateEffectProperty)
            {
                case EStateEffectProperty.TickDamage:
                case EStateEffectProperty.EndDamage:
                    return (baseValue + GetInt(EStateEffectProperty.BonusTickDamage, targetController, specialCondition)) * GetFloat(EStateEffectProperty.BonusTickDamagePerc);

                case EStateEffectProperty.TickHeal:
                    return (baseValue + GetInt(EStateEffectProperty.BonusTickHeal, targetController, specialCondition)) * GetFloat(EStateEffectProperty.BonusHealPerc, targetController, specialCondition: specialCondition);

                case EStateEffectProperty.Damage:
                    return ApplyBonusDamage((int)Mathf.Round(baseValue), targetController, specialCondition);

                case EStateEffectProperty.Heal:
                case EStateEffectProperty.EndHeal:
                    return ApplyBonusHealDealt((int)Mathf.Round(baseValue), targetController, specialCondition);

                case EStateEffectProperty.Shield:
                    return ApplyBonusShield((int)Mathf.Round(baseValue), targetController, specialCondition);

                case EStateEffectProperty.LifeSteal:
                    return baseValue + GetFloat(EStateEffectProperty.BonusLifeSteal) - 1;

                case EStateEffectProperty.BonusTickLifeSteal:
                    return baseValue + GetFloat(EStateEffectProperty.BonusTickLifeSteal) - 1;

                default:
                    return baseValue;
            }
        }

        public int ApplyBonusInt(int baseValue, EStateEffectProperty stateEffectProperty, Controller targetController = null, string specialCondition = "")
        {
            // apply percentage res
            return Math.Max(0, (int)Mathf.Round(ApplyBonus(baseValue, stateEffectProperty, targetController, specialCondition)));
        }

        public void AddExtraEffects(ref SpellData spellData, bool isAutoAttack)
        {
            foreach (StateEffect stateEffect in m_StateEffects)
            {
                if (stateEffect is not SpellEffect spellEffect)
                    continue;

                spellEffect.Apply(ref spellData, isAutoAttack);
            }
        }

        #endregion


        #region Private Manipulators

        /// <summary>
        /// Calculate the total bonus provided by all current state effects
        /// </summary>
        void RecalculateBonus()
        {
            // only server can calculate speed factor
            if (!IsServer)
                return;

            m_SpeedBonus.Value  = GetFloat(EStateEffectProperty.SpeedBonus);

            int baseValue = 0;
            foreach (var effect in m_StateEffects)
            {
                baseValue += effect.RemainingShield;
            }

            if (baseValue < 0)
            {
                ErrorHandler.Warning("Remaining Shield (" + baseValue + ") < 0");
                baseValue = 0;
            }

            m_RemainingShield = baseValue;
            m_Controller.Life.RecalculateShield();
        }

        /// <summary>
        /// Refresh the state effect
        /// </summary>
        /// <param name="stateEffectName"></param>
        void RefreshEffect(string stateEffectName, int level, int stacks = 0)
        {
            foreach (var effect in m_StateEffects)
            {
                if (effect.StateEffectName != stateEffectName)
                    continue;

                effect.Refresh(stacks, level);
                RecalculateBonus();
                return;
            }

            ErrorHandler.Error("Unable to find state effect ("+stateEffectName+") to refresh");
        }

        #endregion


        #region Public Manipulators

        /// <summary>
        /// Add a state effect to the character
        /// </summary>
        /// <param name="stateEffect"></param>
        public void AddStateEffect(string stateEffectName, Controller caster, int level, string origin)
        {
            if (! IsServer)
                return;

            // create and add state effect  
            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectName, level, parent: origin);
            AddStateEffect(stateEffect, caster);
        }

        /// <summary>
        /// Add a state effect to the character
        /// </summary>
        /// <param name="stateEffect"></param>
        public void AddStateEffect(SStateEffectData stateEffectData, Controller caster, int level, string origin)
        {
            if (! IsServer)
                return;

            // create and add state effect  
            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectData.StateEffect.ToString(), level, parent: origin);
            AddStateEffect(stateEffect, caster, stateEffectData);
        }

        /// <summary>
        /// Add a state effect to the character
        /// </summary>
        /// <param name="stateEffect"></param>
        public void AddStateEffect(StateEffect stateEffect, Controller caster, SStateEffectData? overridingData = null)
        {
            if (! IsServer)
                return;

            // calculate number of stacks that need to be applied
            int stacks = overridingData != null ? overridingData.Value.GetStacks() : 1;
            stacks = stateEffect.RecalculateStacks(stacks, caster, m_Controller);

            if (! CheckCanBeApplied(stateEffect, caster))
                return;

            var pastState = GetAnimationState();

            // if already in the list of state effects, refresh it
            if (HasState(stateEffect.StateEffectName))
            {
                if (stateEffect.StateEffectName == "Jump")
                    ErrorHandler.Error("  /!\\ REFRESHING STATE : Jump");

                RefreshEffect(stateEffect.StateEffectName, stateEffect.Level, stacks);
                return;
            }

            // if is UNIQUE : remove all effects of the same type
            if (stateEffect.IsUnique)
                RemoveStateEffectsOfType(stateEffect.StateEffectType);

            // no stacks and no active effect : return
            if (stacks == 0)
                return;

            if (! stateEffect.Initialize(m_Controller, caster, overridingData, stacks))
                return;

            ErrorHandler.Log("Adding state effect " + stateEffect, ELogTag.StateEffects);

            // add the state effect to the list of active effects
            m_StateEffects.Add(stateEffect);

            // recheck bonus potentially provided by this new stateEffect
            RecalculateBonus();

            var currentState = GetAnimationState();
            if (currentState != pastState)
                m_AnimationState.Value = currentState;
        }

        /// <summary>
        /// Add a state effect to the character
        /// </summary>
        /// <param name="type"></param>
        /// <param name="duration"></param>
        public void AddStateEffect(EStateEffect type, Controller caster, string origin, int? stacks = default, float? duration = default)
        {
            if (!IsServer)
                return;

            var overridingProperties = duration.HasValue ? new List<SStateEffectProperty> { new SStateEffectProperty(EStateEffectProperty.Duration, duration.Value) } : new(); 

            AddStateEffect(new SStateEffectData(
                type, 
                stacks:                 stacks      ??      1,
                overridingProperties:   overridingProperties
            ), caster, 1, origin);
        }

        /// <summary>
        /// Remove a state effect from the character
        /// </summary>
        /// <param name="stateEffect"></param>
        public int RemoveStateEffect(string stateEffect, bool consume = false, int maxStacks = -1)
        {
            if (!IsServer)
                return 0;

            // remove effect type from list of active effects
            int index = GetIndexOf(stateEffect);
            if (index == -1)
            {
                ErrorHandler.Error($"Unable to find state {stateEffect} in list");
                return 0;
            }

            // return number of consumed (or removed) stacks
            return m_StateEffects[index].RemoveStacks(maxStacks, consume);
        }

        /// <summary>
        /// Called by the Effect himself to be removed from the list
        /// </summary>
        /// <param name="stateEffect"></param>
        public void RemoveFromList(string stateEffect)
        {
            // remove effect type from list of active effects
            int index = GetIndexOf(stateEffect);
            if (index == -1)
            {
                ErrorHandler.Error($"Unable to find state {stateEffect} in list");
                return;
            } 
            
            // remove effect from list on Server side
            m_StateEffects.RemoveAt(index);

            // recalculate bonuses givent by state effects
            RecalculateBonus();

            // update animation now that state has changed
            m_AnimationState.Value = GetAnimationState();
        }

        /// <summary>
        /// Remove a state effect from the character
        /// </summary>
        /// <param name="state"></param>
        public int RemoveStateEffect(EStateEffect state, bool consume = false, int maxStacks = -1)
        {
            return RemoveStateEffect(state.ToString(), consume, maxStacks);
        }

        public int RemoveStateEffectAtIndex(int index, bool consume = false)
        {
            // check animation state before removing value
            var pastState = GetAnimationState();

            // apply consume effect if requested
            int nStacks = m_StateEffects[index].ForceEnd(consume);

            // remove effect from list on Server side
            m_StateEffects.RemoveAt(index);

            // recalculate bonuses givent by state effects
            RecalculateBonus();

            // update animation now that state has changed
            var currentState = GetAnimationState();
            if (currentState != pastState)
                m_AnimationState.Value = currentState;

            // return number of stacks this spell had (can be used when a spell consumes the state)
            return nStacks;
        }

        public void RemoveStateEffectsOfType(EStateEffectType stateEffectType)
        {
            for (int index = m_StateEffects.Count - 1; index >= 0; index--)
            {
                var stateEffect = m_StateEffects[index];
                if (stateEffect.StateEffectType == stateEffectType)
                {
                    RemoveStateEffectAtIndex(index, true);
                }
            }
        }

        public int GetIndexOf(string stateEffectName)
        {
            return m_StateEffects.FindIndex(stateEffect => stateEffect.StateEffectName == stateEffectName);
        }

        public int GetStacks(EStateEffect state)
        {
            return GetStacks(state.ToString());
        }

        public int GetStacks(string state, bool checkActivated = true)
        {
            if (! HasState(state, checkActivated))
                return 0;

            foreach (var stateEffect in m_StateEffects)
            {
                // skip if not activated (and is requested as such)
                if (checkActivated && ! stateEffect.IsActivated)
                    continue;

                if (stateEffect.StateEffectName == state)
                    return stateEffect.Stacks;
            }

            return 0;
        }

        public EAnimation GetAnimationState()
        {
            if (HasState(EStateEffect.Frozen))
                return EAnimation.Frozen;

            if (IsStunned)
                return EAnimation.Stun;

            if (IsAirborned)
                return EAnimation.Airborne;

            if (IsSilenced)
                return EAnimation.Silenced;

            return EAnimation.None;
        }

        /// <summary>
        /// Hit the shield with some damages and return the remaining damages
        /// </summary>
        /// <param name="damages"></param>
        /// <returns></returns>
        public int HitShield(int damages)
        {
            if (m_RemainingShield == 0)
                return damages;

            var allEffects = m_StateEffects.ToArray();
            foreach (var effect in allEffects)
            {
                // skip if effect no longet exists
                if (!m_StateEffects.Contains(effect)) 
                    continue;

                damages = effect.HitShield(damages);
                if (damages == 0)
                    break;
            }

            RecalculateBonus();

            return damages;
        }

        #endregion


        #region Holding State Effect

        public bool IsHolding(string effect)
        {
            return m_HoldingStateEffects.Contains(effect);
        }

        public void AddHoldingStateEffects(List<string> stateEffects)
        {
            if (stateEffects == null || stateEffects.Count == 0)
                return;

            foreach (var effect in stateEffects)
            {
                m_HoldingStateEffects.Add(effect);
            }
        }

        public void RemoveHoldingStateEffects(List<string> stateEffects)
        {
            if (stateEffects == null || stateEffects.Count == 0)
                return;

            foreach (var effect in stateEffects)
            {
                if (!m_HoldingStateEffects.Contains(effect))
                    continue;
                m_HoldingStateEffects.Remove(effect);
            }
        }

        #endregion


        #region Checkers

        public bool CheckCanBeApplied(StateEffect stateEffect, Controller caster)
        {
            if (IsImmunedToEffects && ! (IsFriendlyEffect(stateEffect) || caster.Team == m_Controller.Team))
                return false;

            if (IsUncontrollable && IsControlEffect(stateEffect.StateEffectName))
                return false;

            return true;
        }

        public bool IsFriendlyEffect(StateEffect stateEffect)
        {
            return stateEffect.IsBuff
                || stateEffect.StateEffectName == EStateEffect.Jump.ToString()
                || stateEffect.StateEffectName == EStateEffect.Invisible.ToString()
                || stateEffect.StateEffectName == EStateEffect.UnTargettable.ToString()
                || stateEffect.StateEffectName == EStateEffect.Invulnerable.ToString()
                || stateEffect.StateEffectName == EStateEffect.Vanish.ToString()
                || stateEffect.StateEffectName == EStateEffect.BlockMovement.ToString()
                || stateEffect.StateEffectName == EStateEffect.BlockCast.ToString()
                || stateEffect.StateEffectName == EStateEffect.SpecialAnimation.ToString();
        }

        public bool IsControlEffect(string stateEffectName)
        {
            return Uncontrollable.CC_EFFECTS.Contains(stateEffectName);
        }

        #endregion


        #region Public Data Accessors

        public float GetFloat(EStateEffectProperty property, Controller targetController = null, bool ignoreConversion = false, string specialCondition = "")
        {
            // only server can calculate speed factor
            if (!IsServer)
                return 1f;

            float value;
            if (property == EStateEffectProperty.SpeedBonus)
                value = 0f;
            else
                value = 1f + m_CharacterData.GetValue(property, specialCondition, m_Controller, targetController);

            ErrorHandler.Log("Base value (" + property + ") : " + value, ELogTag.BonusStats);

            foreach (var effect in m_StateEffects)
            {
                if (! effect.IsActivated || ! effect.HasEffectProperty(property))
                    continue;

                value += effect.GetFloat(property, ignoreConversion, specialCondition: specialCondition);
            }

            ErrorHandler.Log("Final value (" + property + ") : " + value, ELogTag.BonusStats);

            return value;
        }

        public int GetInt(EStateEffectProperty property, Controller targetController = null, string specialCondition = "")
        {
            // only server can calculate speed factor
            if (!IsServer)
                return 0;

            // get BASE VALUE from Character
            int value = m_CharacterData.GetInt(property, specialCondition, m_Controller, targetController);

            ErrorHandler.Log("Base value (" + property + ") : " + value, ELogTag.BonusStats);

            // add EXTRA VALUE from StateEffects
            foreach (var effect in m_StateEffects)
            {
                if (!effect.IsActivated || !effect.HasEffectProperty(property))
                    continue;
                value += effect.GetInt(property, specialCondition: specialCondition);
            }

            ErrorHandler.Log("Final value (" + property + ") : " + value, ELogTag.BonusStats);

            return value;
        }

        #endregion

    }

    [Serializable]
    public struct StateEventData : INetworkSerializable
    {
        public EStateEffectEvent    StateEffectEvent;
        public FixedString64Bytes   StateEffectName;
        public ulong                CasterId;
        public short                Stacks;
        public short                MaxStacks;
        public half                 Duration;

        // Constructor with optional parameters
        public StateEventData(EStateEffectEvent stateEffectEvent, string stateEffectName, ulong casterId, int stacks = 1, int maxStacks = 1, float duration = -1f)
        {
            StateEffectEvent    = stateEffectEvent;
            StateEffectName     = stateEffectName;
            CasterId            = casterId;
            Stacks              = (short)stacks;
            MaxStacks           = (short)maxStacks;
            Duration            = (half)duration;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref StateEffectEvent);
            serializer.SerializeValue(ref StateEffectName);
            serializer.SerializeValue(ref CasterId);
            serializer.SerializeValue(ref Stacks);
            serializer.SerializeValue(ref MaxStacks);

            float tempDuration = (float)Duration;
            serializer.SerializeValue(ref tempDuration);
            Duration = (half)tempDuration;
        }
    }
}