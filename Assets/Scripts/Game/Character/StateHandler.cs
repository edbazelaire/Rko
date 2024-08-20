using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using MyBox;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class StateHandler : NetworkBehaviour
    {
        #region Members
        // ==============================================================================================
        // PRIVATE ACCESSORS
        // -- Network Variables
        NetworkList<FixedString64Bytes>     m_StateEffectList;
        NetworkVariable<float>              m_SpeedBonus = new(1f);
        NetworkVariable<int>                m_RemainingShield = new(0);
        NetworkVariable<EAnimation>         m_AnimationState = new(EAnimation.None);

        // -- SERVER SIDE
        Controller              m_Controller;
        CharacterData           m_CharacterData;
        List<StateEffect>       m_StateEffects;

        // ==============================================================================================
        // PUBLIC ACCESSORS
        public NetworkList<FixedString64Bytes> StateEffectList => m_StateEffectList;
        public bool IsStunned => ! IsUncontrollable
            && (m_StateEffectList.Contains(EStateEffect.Stun.ToString()) 
            || m_StateEffectList.Contains(EStateEffect.Scorched.ToString()));

        public bool IsSilenced => m_StateEffectList.Contains(EStateEffect.Silence.ToString()) 
            || m_StateEffectList.Contains(EStateEffect.Malediction.ToString());

        public bool IsInvulnerable => m_StateEffectList.Contains(EStateEffect.Invulnerable.ToString())
            || m_StateEffectList.Contains(EStateEffect.SpecialAnimation.ToString());
        public bool IsUncontrollable => m_StateEffectList.Contains(EStateEffect.Uncontrollable.ToString())
            || m_StateEffectList.Contains(EStateEffect.SpecialAnimation.ToString());

        public bool IsUnTargetable => m_StateEffectList.Contains(EStateEffect.UnTargettable.ToString())
            || m_StateEffectList.Contains(EStateEffect.SpecialAnimation.ToString())
            || m_StateEffectList.Contains(EStateEffect.Jump.ToString())
            || m_StateEffectList.Contains(EStateEffect.Invisible.ToString());

        public NetworkVariable<float> SpeedBonus => m_SpeedBonus;
        public NetworkVariable<int> RemainingShield => m_RemainingShield;
        public NetworkVariable<EAnimation> AnimationState => m_AnimationState;

        // ==============================================================================================
        // EVENTS
        // used to signal when a state is added / removed
        public event Action<EListEvent, string, int, float> OnStateEvent;
        // used to signal client GFX about spell events
        public event Action<ESpellEvent, string>            OnStateEffectEvent;

        #endregion


        #region Init & End

        private void Awake()
        {
            // init network lists
            m_StateEffectList = new NetworkList<FixedString64Bytes>();

            // init components 
            m_Controller = GetComponent<Controller>();
            m_StateEffects = new List<StateEffect>();   
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        public void Initialize(ECharacter character, int level)
        {
            m_CharacterData = CharacterLoader.GetCharacterData(character, level, destroy: false);
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
                m_StateEffects[i].Update();
                if (!m_Controller.Life.IsAlive)
                    return;
            }
        }

        #endregion


        #region Client RPC

        [ClientRpc]
        public void OnStateEventClientRPC(EListEvent listEvent, string stateEffect, int stacks, float duration)
        {
            if (GameManager.IsGameOver)
                return;

            OnStateEvent?.Invoke(listEvent, stateEffect, stacks, duration);

            if (listEvent == EListEvent.Add)
                SpellLoader.GetStateEffect(stateEffect).PlaySoundEffect();
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(ESpellEvent spellEvent, string stateEffectName)
        {
            ErrorHandler.Log(stateEffectName + " " + spellEvent, ELogTag.StateEffectGFX);

            OnStateEffectEvent?.Invoke(spellEvent, stateEffectName);

            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectName);

            if (stateEffect.VisualEffects == null)
                return;

            foreach (var spawnPrefab in stateEffect.VisualEffects)
            {
                if (spawnPrefab.GFXLifetime.StartSpellPart != spellEvent)
                    continue;

                spawnPrefab.Spawn(null, null, null, stateEffectName, m_Controller, transform.position);
            }
        }

        #endregion


        #region Public Accessors

        public bool HasState(string state)
        {
            if (GameManager.IsGameOver)
                return false;
            return m_StateEffectList.Contains(state);
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
            damages = (int)Mathf.Round(damages * GetFloat(EStateEffectProperty.ResistancePerc));
            
            return damages;
        }

        public int ApplyBonusDamages(int damages)
        {
            // apply percentage res
            damages = Math.Max(0, damages + GetInt(EStateEffectProperty.BonusDamages));

            // apply res fix first
            return (int)Mathf.Round(damages * GetFloat(EStateEffectProperty.BonusDamagesPerc));   
        }

        public int ApplyBonusHeal(int heal)
        {
            // apply percentage res
            heal = Math.Max(0, heal + GetInt(EStateEffectProperty.BonusHeal));

            // apply res fix first
            return (int)Mathf.Round(heal * GetFloat(EStateEffectProperty.BonusHealPerc));   
        }

        public float ApplyBonus(float baseValue, EStateEffectProperty stateEffectProperty)
        {
            switch (stateEffectProperty)
            {
                case EStateEffectProperty.TickDamages:
                    return (baseValue + GetInt(EStateEffectProperty.BonusTickDamages)) * GetFloat(EStateEffectProperty.BonusTickDamagesPerc);

                case EStateEffectProperty.TickHeal:
                    return (baseValue + GetInt(EStateEffectProperty.BonusTickHeal));

                case EStateEffectProperty.Damages:
                case EStateEffectProperty.EndDamages:
                    return ApplyBonusDamages((int)Mathf.Round(baseValue));

                case EStateEffectProperty.Heal:
                case EStateEffectProperty.EndHeal:
                    return ApplyBonusHeal((int)Mathf.Round(baseValue));

                default:
                    return baseValue;
            }
        }

        public int ApplyBonusInt(int baseValue, EStateEffectProperty stateEffectProperty)
        {
            // apply percentage res
            return Math.Max(0, (int)Mathf.Round(ApplyBonus(baseValue, stateEffectProperty)));
        }

        public void AddExtraEffects(ref SpellData spellData, bool isAutoAttack)
        {
            foreach (StateEffect stateEffect in m_StateEffects)
            {
                if (stateEffect is not SpellEffect)
                    continue;

                SpellEffect spellEffect = (SpellEffect)stateEffect;

                if (typeof(AutoAttackEffect) == spellEffect.GetType() && !isAutoAttack)
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

            m_RemainingShield.Value = baseValue;
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
                OnStateEventClientRPC(EListEvent.Add, effect.StateEffectName, effect.Stacks, effect.GetFloat(EStateEffectProperty.Duration));
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
            if (!IsServer)
                return;

            var pastState = GetAnimationState();
            int stacks = overridingData != null ? overridingData.Value.Stacks : 1;

            // if already in the list of state effects, refresh it
            if (HasState(stateEffect.StateEffectName))
            {
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

            Debug.LogWarning("Adding state effect " + stateEffect);

            // add the state effect to the list of active effects
            m_StateEffects.Add(stateEffect);
            m_StateEffectList.Add(stateEffect.StateEffectName);

            // send event to clients (for UI update)
            OnStateEventClientRPC(EListEvent.Add, stateEffect.StateEffectName, stateEffect.Stacks, stateEffect.GetFloat(EStateEffectProperty.Duration));

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
        /// <param name="state"></param>
        public int RemoveStateEffect(string state, bool consume = false, int maxStacks = 0)
        {
            if (!IsServer)
                return 0;

            // remove effect type from list of active effects
            int index = m_StateEffectList.IndexOf(state);
            if (index == -1)
            {
                ErrorHandler.Error($"Unable to find state {state} in list");
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

            // send event to clients (for UI update)
            OnStateEventClientRPC(EListEvent.Remove, m_StateEffects[index].StateEffectName, m_StateEffects[index].Stacks, 0f);

            // remove effect from list on Server side
            Destroy(m_StateEffects[index]);
            m_StateEffectList.RemoveAt(index);
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

        public int GetStacks(EStateEffect state)
        {
            if (! HasState(state))
                return 0;

            foreach (var stateEffect in m_StateEffects)
            {
                if (stateEffect.StateEffectName == state.ToString())
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
            if (m_RemainingShield.Value == 0)
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


        #region Public Data Accessors

        public float GetFloat(EStateEffectProperty property)
        {
            // only server can calculate speed factor
            if (!IsServer)
                return 1f;

            float baseValue;
            if (property == EStateEffectProperty.SpeedBonus)
                baseValue = 1f;
            else
                baseValue = 1f + m_CharacterData.GetValue(property);

            foreach (var effect in m_StateEffects)
            {
                if (! effect.HasProperty(property))
                    continue;
                baseValue += effect.GetFloat(property);
            }

            return baseValue;
        }

        public int GetInt(EStateEffectProperty property)
        {
            // only server can calculate speed factor
            if (!IsServer)
                return 0;

            // get BASE VALUE from Character
            int baseValue = m_CharacterData.GetInt(property);

            // add EXTRA VALUE from StateEffects
            foreach (var effect in m_StateEffects)
            {
                if (!effect.HasProperty(property.ToString()))
                    continue;
                baseValue += effect.GetInt(property);
            }

            return baseValue;
        }

        #endregion

    }
}