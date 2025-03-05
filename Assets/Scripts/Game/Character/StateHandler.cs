using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Character
{
    public class StateHandler : NetworkBehaviour
    {
        #region Members

        // ==============================================================================================
        // EVENTS
        // used to signal when a state is added / removed
        public event Action<EListEvent, string, int, float> StateEffectListEvent;
        // used to signal client GFX about spell events
        public event Action<ESpellEvent, string> StateEffectEvent;

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
            && (HasState(EStateEffect.Stun.ToString()) 
            || HasState(EStateEffect.Scorched.ToString())
            );

        public bool IsAirborned => 
            ! IsUncontrollable
            && (HasState(EStateEffect.Airborne.ToString())
            );

        public bool IsSilenced =>
            HasState(EStateEffect.Silence.ToString()) 
            || HasState(EStateEffect.Malediction.ToString());
        
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

        public bool IsImmuneToEffects => 
            HasState(EStateEffect.SpecialAnimation)
            || HasState(EStateEffect.Vanish.ToString())
;

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


        #region Client RPC

        [ClientRpc]
        public void CallSpellEventClientRPC(SpellEventData spellData)
        {
            // Call UI event
            CallOnStateEventUI(spellData.SpellEvent, spellData.StateEffectName.ToString(), spellData.Stacks, spellData.Duration);

            // Call SpellGFX event
            CallOnStateEffectEvent(spellData.SpellEvent, spellData.StateEffectName.ToString(), spellData.CasterId);
        }

        void CallOnStateEventUI(ESpellEvent spellEvent, string stateEffectName, int stacks, float duration)
        {
            if (spellEvent == ESpellEvent.OnSpawn)
                StateEffectListEvent?.Invoke(EListEvent.Add, stateEffectName, stacks, duration);
            else if (spellEvent == ESpellEvent.OnEnd)
                StateEffectListEvent?.Invoke(EListEvent.Remove, stateEffectName, stacks, duration);
        }

        void CallOnStateEffectEvent(ESpellEvent spellEvent, string stateEffectName, ulong casterId)
        {
            ErrorHandler.Log(stateEffectName + " " + spellEvent, ELogTag.StateEffectGFX);

            StateEffectEvent?.Invoke(spellEvent, stateEffectName);

            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectName);

            if (stateEffect.VisualEffects == null)
                return;

            foreach (var spawnPrefab in stateEffect.VisualEffects)
            {
                if (spawnPrefab.GFXLifetime.StartSpellPart != spellEvent)
                    continue;

                spawnPrefab.Spawn(GameManager.Instance.GetPlayer(casterId), null, null, stateEffectName, m_Controller, transform.position);
            }
        }

        #endregion


        #region Public Accessors

        public bool HasState(string stateEffectName)
        {
            if (GameManager.IsGameOver)
                return false;
            
            return m_StateEffects.FindIndex(stateEffect => stateEffect.StateEffectName == stateEffectName) != -1;
        }

        public bool HasState(EStateEffect state)
        {
            return HasState(state.ToString());
        }

        public void SetStateJump(bool on)
        {
            if (!IsServer)
                return;

            m_Controller.Collider.enabled = !on;

            if (on)
                AddStateEffect(new SStateEffectData(EStateEffect.Jump, overridingProperties: new List<SStateEffectProperty> { new SStateEffectProperty(EStateEffectProperty.Duration, -1f) }), m_Controller);
            else
                RemoveStateEffect(EStateEffect.Jump);
        }

        public int ApplyResistance(int damages)
        {
            // apply res fix first
            damages = Math.Max(0, damages - GetInt(EStateEffectProperty.ResistanceFix));

            // apply percentage res
            damages = (int)Mathf.Round(damages * Mathf.Max(2 - GetFloat(EStateEffectProperty.ResistancePerc), 0));
            
            return damages;
        }

        public int ApplyBonusDamages(int damages, Controller targetController)
        {
            ErrorHandler.Log("Base Damages : " + damages, ELogTag.BonusStats);

            // apply fix bonus damages 
            damages = Math.Max(0, damages + GetInt(EStateEffectProperty.BonusDamages, targetController));

            ErrorHandler.Log("Damages + Fix : " + damages, ELogTag.BonusStats);

            // apply res fix first
            damages = Math.Max(0, (int)Mathf.Round(damages * GetFloat(EStateEffectProperty.BonusDamagesPerc, targetController)));

            ErrorHandler.Log("Final : " + damages, ELogTag.BonusStats);

            return damages;
        }

        public int ApplyBonusHeal(int heal, Controller targetController)
        {
            // apply percentage res
            heal = Math.Max(0, heal + GetInt(EStateEffectProperty.BonusHeal));

            // apply res fix first
            return Math.Max(0, (int)Mathf.Round(heal * GetFloat(EStateEffectProperty.BonusHealPerc, targetController)));   
        }

        public float ApplyBonus(float baseValue, EStateEffectProperty stateEffectProperty, Controller targetController)
        {
            switch (stateEffectProperty)
            {
                case EStateEffectProperty.TickDamages:
                    return (baseValue + GetInt(EStateEffectProperty.BonusTickDamages)) * GetFloat(EStateEffectProperty.BonusTickDamagesPerc);

                case EStateEffectProperty.TickHeal:
                    return (baseValue + GetInt(EStateEffectProperty.BonusTickHeal));

                case EStateEffectProperty.Damages:
                case EStateEffectProperty.EndDamages:
                    return ApplyBonusDamages((int)Mathf.Round(baseValue), targetController);

                case EStateEffectProperty.Heal:
                case EStateEffectProperty.EndHeal:
                    return ApplyBonusHeal((int)Mathf.Round(baseValue), targetController);

                default:
                    return baseValue;
            }
        }

        public int ApplyBonusInt(int baseValue, EStateEffectProperty stateEffectProperty, Controller targetController = null)
        {
            // apply percentage res
            return Math.Max(0, (int)Mathf.Round(ApplyBonus(baseValue, stateEffectProperty, targetController)));
        }

        public void AddExtraEffects(ref SpellData spellData, bool isAutoAttack)
        {
            foreach (StateEffect stateEffect in m_StateEffects)
            {
                if (stateEffect is not SpellEffect spellEffect)
                    continue;

                if (! spellEffect.IsAllowed(spellData.SpellType, isAutoAttack))
                    continue;

                spellData.OnHit.AddRange(spellEffect.OnHits);
                spellData.AllyStateEffects.AddRange(spellEffect.AllyStateEffects);
                spellData.EnemyStateEffects.AddRange(spellEffect.EnemyStateEffects);
            }
        }

        #endregion


        #region Private Manipulators

        /// <summary>
        /// Calculate the total speed bonus provided by all OnHitEffects
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
        public void AddStateEffect(string stateEffectName, Controller caster, int level = 1)
        {
            if (! IsServer)
                return;

            // create and add state effect  
            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectName, level);
            AddStateEffect(stateEffect, caster);
        }

        /// <summary>
        /// Add a state effect to the character
        /// </summary>
        /// <param name="stateEffect"></param>
        public void AddStateEffect(SStateEffectData stateEffectData, Controller caster, int level = 1)
        {
            if (! IsServer)
                return;

            // create and add state effect  
            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectData.StateEffect, level);
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

            if (! CheckCanBeApplied(stateEffect))
                return;

            var pastState = GetAnimationState();
            int stacks = overridingData != null ? overridingData.Value.GetStacks() : 1;

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

            if (! stateEffect.Initialize(m_Controller, caster, overridingData))
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
        public void AddStateEffect(EStateEffect type, Controller caster, int? stacks = default, float? duration = default)
        {
            if (!IsServer)
                return;

            var overridingProperties = duration.HasValue ? new List<SStateEffectProperty> { new SStateEffectProperty(EStateEffectProperty.Duration, duration.Value) } : new(); 

            AddStateEffect(new SStateEffectData(
                type, 
                stacks:                 stacks      ??      1,
                overridingProperties:   overridingProperties
            ), caster);
        }

        /// <summary>
        /// Remove a state effect from the character
        /// </summary>
        /// <param name="stateEffect"></param>
        public int RemoveStateEffect(string stateEffect, bool consume = false, int maxStacks = 0)
        {
            if (!IsServer)
                return 0;

            if (stateEffect == "Invulnerable")
                Debug.LogWarning("  ++ REMOVING STATE : Invulnerable");

            // remove effect type from list of active effects
            int index = GetIndexOf(stateEffect);
            if (index == -1)
            {
                ErrorHandler.Error($"Unable to find state {stateEffect} in list");
                return 0;
            }

            // check if remove effect if not enought stacks 
            if (maxStacks <= 0 || m_StateEffects[index].Stacks < maxStacks)
                return RemoveStateEffectAtIndex(index, consume);     // return number of consumed stacks

            // just retrieve stacks otherwise
            maxStacks = m_StateEffects[index].RemoveStacks(maxStacks);

            // return number of consumed stacks
            return maxStacks;
        }

        /// <summary>
        /// Remove a state effect from the character
        /// </summary>
        /// <param name="state"></param>
        public int RemoveStateEffect(EStateEffect state, bool consume = false, int maxStacks = 0)
        {
            return RemoveStateEffect(state.ToString(), consume, maxStacks);
        }

        public int RemoveStateEffectAtIndex(int index, bool consume = false)
        {
            // check state before removing value
            var pastState = GetAnimationState();

            // keep track of the number of stacks this spell had
            int nStacks = m_StateEffects[index].Stacks;

            // apply consume effect if requested
            if (consume)
            {
                m_StateEffects[index].OnConsumed();
            }

            // remove effect from list on Server side
            Destroy(m_StateEffects[index]);
            m_StateEffects.RemoveAt(index);

            // recalculate bonuses givent by state effects
            RecalculateBonus();

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

                Debug.LogWarning("Remove StateEffect " + stateEffect);
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

        public int GetStacks(string state)
        {
            if (! HasState(state))
                return 0;

            foreach (var stateEffect in m_StateEffects)
            {
                if (stateEffect.StateEffectName == state)
                {
                    return stateEffect.Stacks;
                }
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

            foreach (var effect in m_StateEffects)
            {
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

        public bool CheckCanBeApplied(StateEffect stateEffect)
        {
            if (IsImmuneToEffects && ! IsFriendlyEffect(stateEffect))
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
                || stateEffect.StateEffectName == EStateEffect.SpecialAnimation.ToString();
        }

        #endregion


        #region Public Data Accessors

        public float GetFloat(EStateEffectProperty property, Controller targetController = null)
        {
            // only server can calculate speed factor
            if (!IsServer)
                return 1f;

            float value;
            if (property == EStateEffectProperty.SpeedBonus)
                value = 0f;
            else
                value = 1f + m_CharacterData.GetValue(property, m_Controller, targetController);

            ErrorHandler.Log("Base value (" + property + ") : " + value, ELogTag.BonusStats);

            foreach (var effect in m_StateEffects)
            {
                if (! effect.HasEffectProperty(property))
                    continue;
                value += effect.GetFloat(property);
            }

            ErrorHandler.Log("Final value (" + property + ") : " + value, ELogTag.BonusStats);

            return value;
        }

        public int GetInt(EStateEffectProperty property, Controller targetController = null)
        {
            // only server can calculate speed factor
            if (!IsServer)
                return 0;

            // get BASE VALUE from Character
            int value = m_CharacterData.GetInt(property, m_Controller, targetController);

            ErrorHandler.Log("Base value (" + property + ") : " + value, ELogTag.BonusStats);

            // add EXTRA VALUE from StateEffects
            foreach (var effect in m_StateEffects)
            {
                if (!effect.HasEffectProperty(property))
                    continue;
                value += effect.GetInt(property);
            }

            ErrorHandler.Log("Final value (" + property + ") : " + value, ELogTag.BonusStats);

            return value;
        }

        #endregion

    }

    [Serializable]
    public struct SpellEventData : INetworkSerializable
    {
        public ESpellEvent          SpellEvent;
        public FixedString64Bytes   StateEffectName;
        public ulong                CasterId;
        public byte                 Stacks;
        public half                 Duration;

        // Constructor with optional parameters
        public SpellEventData(ESpellEvent spellEvent, string stateEffectName, ulong casterId, int stacks = 1, float duration = -1f)
        {
            SpellEvent      = spellEvent;
            StateEffectName = stateEffectName;
            CasterId        = casterId;
            Stacks          = (byte)stacks;
            Duration        = (half)duration;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SpellEvent);
            serializer.SerializeValue(ref StateEffectName);
            serializer.SerializeValue(ref CasterId);
            serializer.SerializeValue(ref Stacks);

            float tempDuration = (float)Duration;
            serializer.SerializeValue(ref tempDuration);
            Duration = (half)tempDuration;
        }
    }
}