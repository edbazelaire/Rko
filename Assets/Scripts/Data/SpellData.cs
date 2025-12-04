using Game;
using Game.Spells;
using System.ComponentModel;
using Tools;
using UnityEngine;
using Enums;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using Game.Loaders;
using System;
using System.Collections;
using System.Linq;
using Data.GameManagement;
using System.Reflection;
using MyBox;
using Assets.Scripts.Data.DataStructures;
using Assets.Scripts.Data.DataStructures.SpellRequirement;
using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Assets.Scripts.Game;
using Game.NetworkStructures;
using Data.DataStructures.SpellSubStructures;
using System.Globalization;
using System.Text.RegularExpressions;
using Tools.Helpers;
using Data.DataStructures.SpellSubStructures.SpellModules;
using UnityEngine.Serialization;

namespace Data
{
    [CreateAssetMenu(fileName = "Spell", menuName = "Game/Spells/Default")]
    public class SpellData : CollectableData
    {
        #region Members

        // ===========================================================================
        // Actions
        public Action<Spell> OnSpellSpawn;

        // ===========================================================================
        // Serialized Data
        [Description("Is this spell linked to a specific character")]
        public bool Linked;

        [Header("Prefabs")]
        [Description("Prefab of the spell that will be instantiated when the spell is cast")]
        public GameObject Graphics;

        [Description("List of all Effects appening when the targets")]
        public List<SpellPrefabSpawn> SpellEventActions;

        [Description("SpellData of the spell casted when it hits a target")]
        public List<SpellData> OnHit;

        [Description("List of passive effects of the spell")]
        public List<SSpellPassiveEffect> PassiveEffects;

        [Description("List of effects (spell / stateEffect) activated on a specific spell event")]
        public List<SSpellEventEffect> SpellEventEffects;

        [Description("Where does the OnHit spawns")]
        public ESpellSpawn OnHitSpellSpawn = ESpellSpawn.Ground;

        [Header("Sound Effects")]
        public AudioClip AnimationSoundFX;
        public AudioClip CastSoundFX;
        public AudioClip PermanantSoundFX;
        public AudioClip OnHitSoundFX;
        public AudioClip OnEndSoundFX;

        [Header("Target & Position")]
        [Description("Type of targetting for the spell")]
        public ESpellTarget SpellTarget = ESpellTarget.FirstEnemy;
        [SerializeField, Tooltip("Clamp target position between arena bounds")]
        protected bool m_ClampTargetPos = true;
        [Description("Target offset X/Y")]
        public SOffset TargetOffset = new SOffset(0, 0);
        [Description("Type of targetting for the spell")]
        public ESpellEvent LockTarget = ESpellEvent.OnCast;
        [Description("Is the spell effect applied when NOT hitting the target ?")]
        public bool ApplyIfNotHitting = false;
        [SerializeField, Tooltip("This spell's target position can be changed during the cast animation")]
        public SSpellRelocation SpellRelocation;

        [Header("Requirements")]
        [SerializeField, Tooltip("Is the spell effect applied when NOT hitting the target ?")]
        protected List<SpellRequirements> m_SpellRequirements = new List<SpellRequirements>();

        [Header("Stats")]
        [SerializeField, FormerlySerializedAs("MaxHit"), Tooltip("Maximum number of target that this spell can hit")]
        protected int m_MaxHit = 1;
        [SerializeField, Tooltip("Number of charges for the spell")]
        protected int m_Charges = 1;
        [Tooltip("Energy gained when this spell hits his target")]
        public int EnergyGain = 10;
        [Tooltip("Request amount on energy to be able to cast this spell")]
        public int EnergyCost = 0;

        [SerializeField, Tooltip("All Damage applied by the effect")]
        protected List<SDamage> m_Damages;
        [SerializeField, Tooltip("Heals provided to the target")]
        public int m_Heal = 0;
        [SerializeField, Tooltip("Quantity of (permanant) shield provided to the target")]
        public int m_Shield = 0;
        [SerializeField, Tooltip("Percentage of damages healed on hit")]
        protected float m_LifeSteal = 0f;
        [Description("Max distance of the spell")]
        public float Distance = -1f;
        [Description("List of properties that are overriten on the <OnHit> spells")]
        public List<ESpellProperty> OverrideOnHitProperties;
        [SerializeField, Tooltip("Duration of the spell")]
        public float m_Duration = 0f;
        [Description("Delay of the spell to be instantiated after cast")]
        public float Delay = 0f;
        [SerializeField, Description("Force applied on hitting the target")]
        protected SForce m_Force = default;

        [Header("Scaling")]
        [SerializeField] protected List<SSpellPropertyScaling> m_SpellsScalingLevel = new() {};

        [Header("Position")]
        [SerializeField, Tooltip("Is this spell overrides the default spawn position ?")]
        public bool OverridesSpawnPosition = false;
        [SerializeField, ConditionalField("OverridesSpawnPosition")]
        protected SSpawnPosition m_SpawnPosition;

        [Header("Following")]
        [Tooltip("Is spell following a target ?")]
        public bool IsFollowing = false;
        [ConditionalField("IsFollowing")]
        public MFollowing MFollowing;

        [Header("Collision")]
        [Description("Size of the spell (and hitbox)")]
        [SerializeField] public float m_Size = 1f;
        [Description("Does the spell get trigger on touching a player")]
        public bool TriggerPlayer = true;

        [Header("SolidBody")]
        [Tooltip("Is this a channel spell (ends when caster stops channeling)")]
        public bool HasSolidBody = false;
        [ConditionalField("HasSolidBody")]
        public MSolidBody MSolidBody;

        [Header("Channeling")]
        [Tooltip("Is this a channel spell (ends when caster stops channeling)")]
        public bool     IsChanneling                = false;
        [ConditionalField("IsChanneling")]
        public MChanneling MChanneling;
        [Header("State Effects")]
        [Description("List of effects that proc on hitting an enemy")]
        public List<SStateEffectData> EnemyStateEffects;
        [Description("List of effects that proc on hitting an ally")]
        public List<SStateEffectData> AllyStateEffects;
        [Description("The stats are multiplied by the number of stacks present on the enemy target")]
        public EStateEffect StateEffectStackFactor;

        [Header("Graphics")]
        [Description("Delay for the spell visual to be deleted after end of the spell")]
        public float PersistanceAfterEnd = 0f;

        [Header("Animation & Cooldowns")]
        [Description("Name of the animation to use")]
        public EAnimation Animation;
        [Description("Can the animation be cancelled by the player ?")]
        public bool IsCancellable = false;
        [Description("Can the animation be interrupted by the enemy ?")]
        public bool IsInterruptable = true;
        [Description("Can the spell be casted through CC ?")]
        public bool IgnoreCC = false;
        [Description("Time for the animation to take from start to begin (in seconds)")]
        public float AnimationTimer;
        [Description("Cooldown to be able to re-use that ability")]
        [SerializeField] protected float m_Cooldown;

        // ===========================================================================
        // Local private variables
        /// <summary> name of the parent spell (to retrieve the value in the damages count) </summary>
        protected string m_Parent;
        /// <summary> id of the current target, that can be set to loc a specific target (e.g : OnHit effects) </summary>
        protected ulong? m_CurrentTargetId = null;
        /// <summary> list of overriding data </summary>
        protected List<(ESpellProperty Property, float Value)> m_OverridingData;

        // ===========================================================================
        // Dependent Members
        public override ERarety Rarety          => GetRarety();
        public virtual string Parent            => m_Parent.IsNullOrEmpty() ? Name : m_Parent;
        public ESpell Spell                     => Id == null ? ESpell.None : (ESpell)Id;
        public ulong? CurrentTargetId           => m_CurrentTargetId;
        public virtual List<SpellRequirements> SpellRequirements => m_SpellRequirements;
        public SSpawnPosition SpawnPosition     => m_SpawnPosition;
        public virtual ESpellType SpellType     => ESpellType.InstantSpell;
        public List<SSpellPropertyScaling> SpellsScalingLevel => m_SpellsScalingLevel;
        public float BaseSize                   => m_Size;
        public float Size                       => m_Size >= 0 ? m_Size * Settings.SpellSizeFactor : ArenaManager.Instance.TargettableAreaSize;
        protected override Type m_EnumType      => typeof(ESpell);
        public bool ClampTargetPos              => m_ClampTargetPos;
        public bool IsAutoTarget                => true;
        public bool IsEnemyTarget               => TargetHelper.IsEnemyTarget(SpellTarget);
        public bool IsAllyTarget                => TargetHelper.IsAllyTarget(SpellTarget);

        // ===========================================================================
        // Level Dependent Members
        public virtual int MaxHit               => (int)GetScaledValue(ESpellProperty.MaxHit, m_MaxHit);
        public virtual int Charges              => (int)GetScaledValue(ESpellProperty.Charges, m_Charges);
        public virtual float Cooldown           => GetScaledValue(ESpellProperty.Cooldown, m_Cooldown);
        public virtual List<SDamage> Damages    => m_Damages;
        public virtual int Heal                 => (int)GetScaledValue(ESpellProperty.Heal, m_Heal);
        public virtual int Shield               => (int)GetScaledValue(ESpellProperty.Shield, m_Shield);
        public virtual float LifeSteal          => GetScaledValue(ESpellProperty.LifeSteal, m_LifeSteal);
        public virtual float Duration           => GetScaledValue(ESpellProperty.Duration, m_Duration);
        public virtual SForce Force             => m_Force;

        /// <summary> is the "IsCasting" over once the spell has been casted (before delay) ? </summary>
        public virtual bool IsCompletedOnCast   => true;
        public virtual EHitCategory SpellCategory => EHitCategory.Direct;

        #endregion


        #region Instantiate & Spawns

        /// <summary>
        /// Cast a the spell with a delay
        /// </summary>
        /// <param name="clientId">     caster of the spell                         </param>
        /// <param name="target">       position targetted                          </param>
        /// <param name="position">     position where to spawn the spell prefab    </param>
        /// <param name="rotation">     rotation of the prefab                      </param>
        /// <returns></returns>
        public IEnumerator CastDelay(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default, float? delay = null, bool recalculateTarget = true, bool recalculatePosition = true)
        {
            if (delay == null)
                delay = Delay;

            // recalculate target depending on spell type
            if (recalculateTarget)
                CalculateTarget(ref target, clientId, m_CurrentTargetId);

            // send event of "OnCast"
            CallSpellEvent(GameManager.Instance.GetPlayer(clientId), ESpellEvent.OnCast, target);

            // wait end of delay
            while (delay > 0f)
            {
                // if gameOver : exit
                if (GameManager.IsGameOver)
                    yield break;

                delay -= Time.deltaTime;
                yield return null;
            }

            // cast the spell at the end of the delay
            bool recalculateOnCast = LockTarget == ESpellEvent.OnSpawn;

            Cast(clientId, target, position, rotation, recalculateTarget: recalculateOnCast, recalculatePosition: recalculatePosition);
        }

        /// <summary>
        /// Cast a the spell by instantiating the prefab, initializing it and spawning in the network
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="recalculateTarget"></param>
        public virtual void Cast(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default, bool recalculateTarget = true, bool recalculatePosition = true, bool recalculateRotation = true)
        {
            // recalculate target if required
            if (recalculateTarget)
                CalculateTarget(ref target, clientId, m_CurrentTargetId);

            if (recalculatePosition)
                RecalculatePosition(ref position, target, clientId);

            if (recalculateRotation)
                RecalculateRotation(ref rotation);

            // remove all CC if is UnstoppableCast
            ApplyIgnoreCC(GameManager.Instance.GetPlayer(clientId));

            // instantiate the prefab of the spell
            Transform parent = FindParent(clientId);
            GameObject spellGO = PoolManager.Pool(GetSpellPrefab(), clientId, position, rotation, parent);

            // initialize the spell
            var spell = Finder.FindComponent<Spell>(spellGO);
            spell.Initialize(clientId, target, this);

            // backpropagate the spell intialization to the client (for the preview)
            if (! GameManager.Instance.IsOfflineMode)
                spell.InitializeClientRpc(clientId, new Vector2Short(target), Name, (byte)m_Level);

            // call event that spell spawned
            OnSpellSpawn?.Invoke(spell);
            CallSpellEvent(GameManager.Instance.GetPlayer(clientId), ESpellEvent.OnSpawn, target);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>q
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void SpawnOnHitPrefab(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default)
        {
            if (OnHit == null || OnHit.Count == 0)
                return;

            foreach(SpellData spellData in OnHit)
            {
                SubCastSpell(spellData, clientId, null, null, target, position, rotation);
            }
        }

        /// <summary>
        /// Cast a spell from this spell (apply some overrides, update level, ...)
        /// </summary>
        public void SubCastSpell(SpellData subSpellData, ulong casterId, ulong? targetId = null, ESpellTarget? spellTarget = null, Vector3 targetPos = default, Vector3 position = default, Quaternion rotation = default, bool recalculateTarget = false, bool recalculatePosition = false)
        {
            var controller = GameManager.Instance.GetPlayer(casterId);

            // setup spell data to level of this spell
            var spellData = subSpellData.Clone(m_Level);
            spellData.SetParent(Parent);
            spellData.SetCurrentTargetId(targetId.HasValue ? targetId : m_CurrentTargetId);

            // override target if requested
            if (spellTarget.HasValue)
                spellData.SpellTarget = spellTarget.Value;

            if (spellData.SpellTarget == ESpellTarget.None)
                targetPos = position;

            // call override
            spellData.Override(this);
            spellData.OverrideSpellSpawn(OnHitSpellSpawn);
            controller.StartCoroutine(spellData.CastDelay(casterId, targetPos, position, rotation, recalculateTarget: recalculateTarget, recalculatePosition: recalculatePosition));

            // call graphics event
            controller.SpellHandler.CallSpellEvent(spellData.Name, ESpellEvent.OnStartCast, level: m_Level, targetPosition: targetPos);
        }

        void ApplyIgnoreCC(Controller caster)
        {
            if (!IgnoreCC)
                return;

            caster.StateHandler.RemoveAllCC();
        }

        #endregion


        #region Event Sub Effects

        /// <summary>
        /// Cast all spells and effects at requested Event
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="level"></param>
        /// <param name="parent"></param>
        /// <param name="caster"></param>
        /// <param name="targetController"></param>
        /// <param name="targetPosition"></param>
        /// <param name="position"></param>
        public void CallSubEffects(ESpellEvent spellEvent, int level, string parent, Controller caster, Controller targetController = null, Vector2 targetPosition = default, Vector2 position = default)
        {
            // ======================================================================================
            // SPAWN SUB EFFECTS
            foreach (SSpellEventEffect spellEventEffect in SpellEventEffects)
            {
                if (spellEventEffect.SpellEvent != spellEvent)
                    continue;

                if (CheckSpellEventEffectSpecialCase(spellEventEffect, caster)) 
                { 
                    // ...
                }
                else if (SpellLoader.IsSpell(spellEventEffect.EffectName))
                {
                    SubCastSpell(
                        subSpellData:           SpellLoader.GetSpellData(spellEventEffect.EffectName, level),
                        casterId:               caster.PlayerId,
                        targetId:               targetController != null ? targetController.PlayerId : null,
                        spellTarget:            spellEventEffect.SpellTarget,
                        position:               position,
                        targetPos:              targetPosition,
                        recalculatePosition:    true
                    );
                }

                else if (SpellLoader.IsStateEffect(spellEventEffect.EffectName))
                {
                    Controller finalTargetController = GetTargetController(
                        casterId:       caster.PlayerId,
                        spellTarget:    spellEventEffect.SpellTarget,
                        targetId:       targetController != null ? targetController.PlayerId : null
                    );

                    if (finalTargetController == null)
                    {
                        ErrorHandler.Warning("Unable to find controller for stateEffect " + spellEventEffect.EffectName + " of spell " + Name);
                        return;
                    }

                    finalTargetController.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(spellEventEffect.EffectName, level, parent: parent), caster);
                }

                else
                {
                    ErrorHandler.Error("Unable to find " + spellEventEffect.EffectName + " as Spell or StateEffect");
                    continue;
                }
            }
        }

        bool CheckSpellEventEffectSpecialCase(SSpellEventEffect spellEventEffect, Controller caster)
        {
            if (CheckSpecialCase_Dash(spellEventEffect, out float speed, out float duration))
            {
                caster.Movement.AddForce(new SForce(speed, duration, null));
                return true;
            }
            return false;
        }

        bool CheckSpecialCase_Dash(SSpellEventEffect spellEventEffect, out float speed, out float duration)
        {
            string pattern = @"^Dash\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*\)$";
            var match = Regex.Match(spellEventEffect.EffectName, pattern);

            speed = 0f; 
            duration = 0f;

            if (!match.Success)
                return false;

            // Utiliser InvariantCulture pour éviter les problèmes de virgules/décimales
            if (float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out speed) &&
                float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out duration))
            {
                return true;
            }

            return false;
        }

        #endregion


        #region Spell GFX

        public void CallSpellEvent(Controller controller, ESpellEvent spellEvent, Vector3 target)
        {
            if (controller == null)
                return;

            // spawn SubSpell - SpellGFX
            controller.SpellHandler.CallSpellEvent(Name, spellEvent, m_Level, targetPosition: target);
        }

        public bool HasEventAt(ESpellEvent spellEvent, bool checkStart = true, bool checkEnd = true)
        {
            if (HasGfxEventAt(spellEvent, checkStart, checkEnd))
                return true;

            return HasSpellRelocationEventAt(spellEvent, checkStart, checkEnd);
        }

        public bool HasSpellRelocationEventAt(ESpellEvent spellEvent, bool checkStart = true, bool checkEnd = true)
        {
            if (SpellRelocation.Lifetime == null)
                return false;

            // is starting
            if (checkStart && SpellRelocation.Lifetime.StartSpellPart != ESpellEvent.None && SpellRelocation.Lifetime.StartSpellPart == spellEvent)
                return true;

            // spell has "End" event and current event is this event or higher
            if (checkEnd && SpellRelocation.Lifetime.EndSpellPart != ESpellEvent.None && spellEvent >= SpellRelocation.Lifetime.EndSpellPart)
                return true;

            return false;
        }

        /// <summary>
        /// Check if spell is currently relovation 
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        public bool HasOnGoingSpellRelocationEventAt(ESpellEvent spellEvent)
        {
            if (SpellRelocation.Lifetime == null)
                return false;

            // check is in between start and end
            return SpellRelocation.Lifetime.StartSpellPart <= spellEvent && spellEvent <= SpellRelocation.Lifetime.EndSpellPart;
        }

        /// <summary>
        /// Check if this spell has GFX event linked to this event
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        public bool HasGfxEventAt(ESpellEvent spellEvent, bool checkStart = true, bool checkEnd = true)
        {
            foreach (var action in SpellEventActions)
            {
                // is starting
                if (checkStart && action.GFXLifetime.StartSpellPart == spellEvent)
                    return true;

                // spell has "End" event and current event is this event or higher
                if (checkEnd && action.GFXLifetime.EndSpellPart != ESpellEvent.None && spellEvent >= action.GFXLifetime.EndSpellPart)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if this spell has GFX event requesting the target position
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        public bool HasTargetGfxEventAt(ESpellEvent spellEvent)
        {
            foreach (var action in SpellEventActions)
            {
                // is starting
                if (action.GFXLifetime.StartSpellPart == spellEvent && action.SpawnTarget == ESpawnTarget.TargetPos)
                    return true;
            }

            return false;
        }

        #endregion


        #region Overriders 

        public void Override(SpellData overridingData)
        {
            if (overridingData.OverrideOnHitProperties.Count == 0)
                return;

            ErrorHandler.Log(() => "Overriding data of " + Name + " with " + overridingData.Name, ELogTag.Spells);

            foreach (ESpellProperty spellProperty in overridingData.OverrideOnHitProperties)
            {
                ErrorHandler.Log(() => "      + " + spellProperty + " : ", ELogTag.Spells);
                ErrorHandler.Log(() => "          - FROM : " + GetProperty(spellProperty), ELogTag.Spells);

                SetProperty(spellProperty, overridingData.GetProperty(spellProperty));

                ErrorHandler.Log(() => "          - TO : " + GetProperty(spellProperty), ELogTag.Spells);
            }
        }

        public virtual void OverrideSpellSpawn(ESpellSpawn spellSpawn) { }

        #endregion


        #region Spell Helpers

        protected virtual GameObject GetSpellPrefabOffline()
        {
            return SpellLoader.GetSpellPrefab(Name, SpellType);
        }

        protected virtual NetworkObject GetSpellPrefab()
        {
            return SpellLoader.GetSpellPrefab(Name, SpellType).GetComponent<NetworkObject>();
        }

        /// <summary>
        /// Does the spell (or its sub spells) has the expected state effect
        /// </summary>
        /// <param name="stateEffect"></param>
        /// <returns></returns>
        public bool HasStateEffect(EStateEffect stateEffect)
        {
            if (EnemyStateEffects.Where(t => t.StateEffect == stateEffect).Count() > 0)
                return true;

            foreach (var subSpellData in OnHit)
            {
                if (subSpellData.HasStateEffect(stateEffect))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Re order state effects by application priority
        /// </summary>
        /// <param name="stateEffects"></param>
        public virtual List<SStateEffectData> ReOrderStateEffects(List<SStateEffectData> stateEffects)
        {
            // Step 1: Calculate the effective priority for each state effect
            var stateEffectWithPriority = stateEffects.Select(effect =>
            {
                int priority = 0;

                // check if has overriding priority
                int index = effect.OverridingProperties.FirstIndex(property => property.StateEffectProperty == EStateEffectProperty.Priority);
                if (index == -1)
                    priority = SpellLoader.GetStateEffect(effect.StateEffect.ToString()).Priority;
                else
                    priority = (int)effect.OverridingProperties[index].Value;

                // Return a tuple containing the original effect and its calculated priority
                return (Effect: effect, Priority: priority);
            });

            // Step 2: Sort the effects by priority in descending order (highest priority first)
            var sortedStateEffects = stateEffectWithPriority
                .OrderByDescending(item => item.Priority)
                .Select(item => item.Effect)
                .ToList();

            return sortedStateEffects;
        }

        public virtual Controller GetTargetController(ulong casterId, ESpellTarget spellTarget, ulong? targetId = null, bool throwError = true)
        {
            Controller controller = GameManager.Instance.GetPlayer(casterId);

            switch (spellTarget)
            {
                case ESpellTarget.Self:
                    return controller;

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(controller.Team, casterId);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(controller.Team);

                case ESpellTarget.CurrentTarget:
                    if (targetId == null)
                    {
                        ErrorHandler.Error("SpellTarget of " + Name + " is CurrentTarget but no target id was provided");
                        return null;
                    }
                    return GameManager.Instance.GetPlayer(targetId.Value);

                default:
                    if (throwError)
                        ErrorHandler.Warning("No controller found for target of type " + spellTarget);
                    return null;
            }
        }

        public virtual void CalculateTarget(ref Vector3 target, ulong casterId, ulong? targetId = null) 
        {
            if (!IsAutoTarget)
                return;

            TargetHelper.GetTargetPosition(ref target, SpellTarget, TargetOffset.AsVector2, m_ClampTargetPos, casterId, targetId);
        }

        public virtual void ClampTargetX(ref Vector3 target, ulong clientId) 
        {
            TargetHelper.ClampTargetX(ref target, SpellTarget, clientId);
        }

        public Transform GetTargettableArea(int team)
        {
            return TargetHelper.GetTargettableArea(team, SpellTarget);
        }

        /// <summary>
        /// Method for children to change parent spawning if need be
        /// </summary>
        /// <returns></returns>
        protected virtual Transform FindParent(ulong clientId)
        {
            return null;
        }

        protected virtual ERarety GetRarety()
        {
            if (!Linked) 
                return m_Rarety;

            string character = CharacterLoader.GetCharacterNameWithSpell(Spell);
            if (character.IsNullOrEmpty())
            {
                ErrorHandler.Error("Unable to find character with spell " + Spell);
                return ERarety.Common;
            }

            var charData = CharacterLoader.GetCharacterData(character);
            if (charData.AutoAttack == Spell)
                return ERarety.Common;
            if (charData.SpecialAbility == Spell)
                return ERarety.Rare;
            if (charData.Ultimate == Spell)
                return ERarety.Legendary;

            ErrorHandler.Error("Unable to find rarety of linked spell " + Spell);
            return ERarety.Common;
        }

        #endregion


        #region Target | Position | Rotation

        public virtual void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId) 
        {
            if (! OverridesSpawnPosition)
            {
                position = GameManager.Instance.GetPlayer(clientId).GFXHandler.GetSpellSpawn().position;
                return;
            }

            var caster = GameManager.Instance.GetPlayer(clientId);
            var parent = m_SpawnPosition.CalculateParent(caster, spell: null, targetController: null);
            position = m_SpawnPosition.CalculatePosition(parent, caster, callFromPosition: position, targetPosition: target);
        }

        public virtual void RecalculateRotation(ref Quaternion rotation)
        {
            rotation = Quaternion.identity;
        }

        #endregion


        #region Properties by Reflection

        /// <summary>
        /// Get Reflection PropertyInfo of desire StateEffect property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected bool TryGetPropertyInfo(ESpellProperty property, out FieldInfo propertyInfo, bool throwError = true)
        {
            // Get the type of MyClass
            Type myType = this.GetType();

            string propertyName = property.ToString();

            // handle special cases first
            switch (property)
            {
                case ESpellProperty.Damage:
                    propertyName = "m_Damage";
                    break;

                case ESpellProperty.Heal:
                    propertyName = "m_Heal";
                    break;
            }

            propertyInfo = myType.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null)
                return true;

                        // try get property with "m_"
            propertyInfo = myType.GetField("m_"+propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null)
                return true;

            if (throwError)
                ErrorHandler.Error("Unknown property " + property + " for Spell " + name);

            return false;
        }

        /// <summary>
        /// Get Reflection PropertyInfo of desire property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected bool TryGetPropertyInfo(ESpellProperty property, out PropertyInfo propertyInfo, bool throwError = true)
        {
            // Get the type of MyClass
            Type myType = this.GetType();

            string propertyName = property.ToString();

            // check to get Dependent Properties
            propertyInfo = myType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo != null)
                return true;

            if (throwError)
                ErrorHandler.Error("Unknown property " + property + " for Spell " + name);

            return false;
        }

        /// <summary>
        /// Set the value of a property by Reflection
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        protected virtual void SetProperty(ESpellProperty property, object value)
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
        public virtual bool TryGetProperty(ESpellProperty property, out object value, bool throwError = false)
        {
            value = null;

            // CHECK : Fields
            if (TryGetPropertyInfo(property, out FieldInfo fieldInfo, false))
            {
                value = fieldInfo.GetValue(this);
                return true;
            }

            // CHECK : Properties (dependent, etc..)
            if (TryGetPropertyInfo(property, out PropertyInfo propertyInfo, throwError))
            {
                value = propertyInfo.GetValue(this);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the value of a property by Reflection
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual object GetProperty(ESpellProperty property, bool throwError = true)
        {
            if (TryGetPropertyInfo(property, out FieldInfo fieldInfo, false))
                return fieldInfo.GetValue(this);

            if (TryGetPropertyInfo(property, out PropertyInfo propertyInfo, throwError))
                return propertyInfo.GetValue(this);

            return null;
        }

        public virtual float GetFloat(ESpellProperty property)
        {
            object value = GetProperty(property);
            if (value == null)
                return default;

            if (! float.TryParse(value.ToString(), out float result))
            {
                ErrorHandler.Error("Unable to parse in FLOAT value " + value + " of property " + property + " of spell " + name);
                return default;
            }

            return result;
        }

        public virtual int GetInt(ESpellProperty property)
        {
            object value = GetProperty(property);
            if (value == null)
                return default;

            if (! int.TryParse(value.ToString(), out int result))
            {
                ErrorHandler.Error("Unable to parse in FLOAT value " + value + " of property " + property + " of spell " + name);
                return default;
            }

            return result;
        }

        public virtual T GetProperty<T>(ESpellProperty property)
        {
            object value = GetProperty(property);
            if (value == null)
                return default;

            try
            {
                return (T)value;
            }
            catch (Exception ex)
            {
                ErrorHandler.Error(ex.Message);
                ErrorHandler.Error("Unable to parse value " + value + " of property " + property + " of spell " + name);
                return default;
            }
        }


        #endregion


        #region Infos & Description

        public override Dictionary<string, object> GetInfo()
        {
            var infosDict = base.GetInfo();
            
            infosDict.Add("Type", GetTypeInfo());
            infosDict.Add("Target", GetTargetTypeInfo());

            if (IgnoreCC)
                infosDict.Add("IgnoreCC", Cooldown);

            if (Cooldown > 0)
                infosDict.Add("Cooldown", Cooldown);
            if (Charges > 1 || IsScalingProperty(ESpellProperty.Charges.ToString(), out EScalingDirection _))
                infosDict.Add("Charges", Charges);
            if (EnergyCost > 0)
                infosDict.Add("EnergyCost", EnergyCost);
            if (SpellRequirements.Count > 0)
                infosDict.Add("SpellRequirements", SpellRequirements);

            if (! Damages.IsNullOrEmpty())
                infosDict.Add("Damages", Damages);
            if (LifeSteal > 0)
                infosDict.Add("LifeSteal", LifeSteal);

            if (Heal > 0)
                infosDict.Add("Heal", Heal);
            if (Shield > 0)
                infosDict.Add("Shield", Shield);
            if (Duration > 0)
                infosDict.Add("Duration", Duration);
            if (EnergyGain > 0)
                infosDict.Add("Energy", EnergyGain);
            if (m_Size > 0 && m_Size != 1)
                infosDict.Add("Size", m_Size);
            if (SpellRelocation.Exists && SpellRelocation.Lifetime.StartSpellPart >= ESpellEvent.OnSpawn)
                infosDict.Add("Movement", "Manual");

            infosDict.Add("CastDuration",   AnimationTimer);

            if (Delay > 0)
                infosDict.Add("Delay", Delay);

            if (Distance > 0)
                infosDict.Add("Distance", Distance);

            // add state effects
            AddStateEffectInfos(ref infosDict);
            
            // add on hit infos if has any
            AddOnHitInfos(ref infosDict);
            
            return infosDict;
        }

        /// <summary>
        /// Get the description of the spell
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            var description = base.GetDescription();

            // TODO : Better ==================================================================
            // todo - handle multiple sub spells
            if (OnHit.Count() > 0)
                description = TextHandler.ReplaceSubSpellData(description, OnHit[0]);
            if (SpellEventEffects.Count > 0 && SpellLoader.IsSpell(SpellEventEffects[0].EffectName))
                description = TextHandler.ReplaceSubSpellData(description, SpellLoader.GetSpellData(SpellEventEffects[0].EffectName, m_Level));
            // TODO : Better ==================================================================

            description = TextHandler.ReplaceSubStateEffects(description, this);
            description = TextHandler.ReplacePassiveEffects(description, this);
            description = TextHandler.ReplaceSpellRequirements(description, this);

            return description;
        }

        /// <summary>
        /// Convert a description variable into a string implemented into the description
        /// </summary>
        /// <returns></returns>
        public override string ConvertDescriptionVariable(SDescriptionVariable descriptionVariable, Dictionary<string, object> infos = default, bool throwError = true)
        {
            string stringValue = base.ConvertDescriptionVariable(descriptionVariable, infos, throwError: false);
            if (stringValue != TextHandler.UNDEFINED)
                return stringValue;   

            if (Enum.TryParse(descriptionVariable.Name, out ESpellProperty property))
            {
                if (!TryGetProperty(property, out object value))
                {
                    ErrorHandler.Error("Unable to find property " + property + " in spell " + Name);
                    return TextHandler.UNDEFINED;
                }

                if (float.TryParse(value.ToString(), out float floatValue))
                    value = TextHandler.FormatPropertyValue(floatValue, descriptionVariable.Name);

                return TextHandler.FormatPropertyIcon(descriptionVariable.Name, value, true, false);
            }

            return TextHandler.UNDEFINED;
        }

        /// <summary>
        /// Add on hit infos to infos dictionnary
        /// </summary>
        /// <param name="infosDict"></param>
        /// <returns></returns>
        void AddOnHitInfos(ref Dictionary<string, object> infosDict)
        {
            if (OnHit.Count > 0)
            {
                if (OnHit.Count > 1)
                    ErrorHandler.Warning("OnHit.Count > 1 in "+Name+" : this case is not handled in infos description");

                OnHit[0].AddAsSubSpellInfos(ref infosDict);
                return;
            }

            if (SpellEventEffects.Count > 0)
            {
                if (SpellEventEffects.Count > 1)
                    ErrorHandler.Warning("SpellEventEffects.Count > 1 in "+Name+" : this case is not handled in infos description");

                var effect = SpellEventEffects[0].EffectName;
                if (SpellLoader.IsSpell(effect))
                {
                    SpellLoader.GetSpellData(effect, m_Level).AddAsSubSpellInfos(ref infosDict);
                    return;
                } else
                {
                    // TODO : Handle none spell effects in description ?
                    ErrorHandler.Warning("Unhandled case : " + effect);
                }
            }
        }

        public void AddAsSubSpellInfos(ref Dictionary<string, object> infosDict)
        {
            string[] keysToIgnore = new string[] { "Type", "Target", "Cooldown", "CastDuration", "Distance", "EnergyCost", "Delay", "Charges" };        // keys to ignore as overwrite  
            string[] keysToAdd = new string[] { "Damages", "PhysicalDamage", "MagicalDamage", "ExecutionDamage", "Heal", "Shield", "DotDamage", "DotHeal", "Effects" };                                // keys that are not overritten but additionned 

            var subSpellInfos = GetInfo();
            foreach (var info in subSpellInfos)
            {
                // skip some keys
                if (keysToIgnore.Contains(info.Key))
                    continue;

                // keys overriden by the base spell
                if (OverrideOnHitProperties.Any(property => property.ToString().Equals(info.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (keysToAdd.Contains(info.Key))
                {
                    if (info.Key == "Effects")
                    {
                        if (!infosDict.ContainsKey(info.Key))
                            infosDict[info.Key] = new List<SStateEffectData>();
                        ((List<SStateEffectData>)infosDict[info.Key]).AddRange((List<SStateEffectData>)info.Value);
                        continue;
                    }

                    if (info.Key == "Damages")
                    {
                        if (!infosDict.ContainsKey(info.Key))
                            infosDict[info.Key] = new List<SDamage>();
                        ((List<SDamage>)infosDict[info.Key]).AddRange((List<SDamage>)info.Value);
                        continue;
                    }

                    // DEFAULT BEHAVIOR : must be a float
                    float baseValue = 0f;
                    if (infosDict.ContainsKey(info.Key) && !float.TryParse(infosDict[info.Key].ToString(), out baseValue))
                    {
                        ErrorHandler.Error("Unable to parse " + info.Key + " with value " + infosDict[info.Key] + " in base spell of " + Spell);
                        continue;
                    }

                    if (!float.TryParse(info.Value.ToString(), out float subSpellValue))
                    {
                        ErrorHandler.Error("Unable to parse " + info.Key + " with value " + info.Value + " in spell " + Spell);
                        continue;
                    }

                    infosDict[info.Key] = baseValue + subSpellValue;
                    continue;
                }

                infosDict[info.Key] = info.Value;
            }
        }

        void AddStateEffectInfos(ref Dictionary<string, object> infosDict)
        {
            if (AllyStateEffects.Count == 0 && EnemyStateEffects.Count == 0)
                return;

            if (! infosDict.ContainsKey("Effects"))
                infosDict.Add("Effects", new List<SStateEffectData>());
            
            foreach (var stateEffect in AllyStateEffects)
            {
                (infosDict["Effects"] as List<SStateEffectData>).Add(stateEffect);
            }
            foreach (var stateEffect in EnemyStateEffects)
            {
                (infosDict["Effects"] as List<SStateEffectData>).Add(stateEffect);
            }
        }

        public virtual string GetTypeInfo()
        {
            return SpellType.ToString();
        }

        public string GetTargetTypeInfo()
        {
            if (SpellRelocation.Lifetime != null && (SpellRelocation.Lifetime.StartSpellPart != ESpellEvent.None && SpellRelocation.Lifetime.StartSpellPart < ESpellEvent.OnSpawn))
                return "Redirectable";

            switch (SpellTarget)
            {
                case ESpellTarget.Fixed:
                case ESpellTarget.Mirror:
                case ESpellTarget.Self:
                    return SpellTarget.ToString();

                default:
                    return "Auto";
            }
        }

        public override bool IsScalingProperty(string propertyName, out EScalingDirection scaling)
        {
            if (base.IsScalingProperty(propertyName, out scaling)) 
                return true;

            // not a defined SpellProperty = not scaling value
            ESpellProperty property;
            if (propertyName == "Projectiles")
                property = ESpellProperty.NProjectiles;
            else if (propertyName == "CastDuration")
                property = ESpellProperty.AnimationTimer;
            else if (!Enum.TryParse(propertyName, out property))
                return false;

            // check sub spells to see if property is scaling there
            foreach (SpellData onHitSpellData in OnHit)
            {
                if (onHitSpellData.IsScalingProperty(propertyName, out scaling))
                    return true;
            }

            // check propery is in scaling properties
            int index = m_SpellsScalingLevel.FirstIndex(scaling => scaling.Property == property);
            if (index < 0)
                return false;

            // get type of scaling for this property
            SSpellPropertyScaling scalingProperty = m_SpellsScalingLevel[index];
            if (scalingProperty.Value > 0)
            {
                scaling = EScalingDirection.Up;
                return true;
            }

            if (scalingProperty.Value < 0)
            {
                scaling = EScalingDirection.Down;
                return true;
            }

            // -- getting the scaling direction of a threshold value can be challenging : lets assume that all thresholds are "UP"
            if (scalingProperty.ScalingTresholds.Count > 0)
            {
                scaling = EScalingDirection.Up;
                return true;
            }

            // CHECK : Unhandled case
            ErrorHandler.Warning($"Unhandled case in spell {Name} : property {property} is in m_SpellsScalingLevel but unable to interpret the type of scaling");
            return false;
        }

        #endregion


        #region Level management

        public new SpellData Clone(int level = 0, bool destroy = false)
        {
            // clone this spell
            SpellData spellData = (SpellData)base.Clone(level == 0 ? m_Level : level, destroy);

            // -- copy current target id
            spellData.SetCurrentTargetId(CurrentTargetId);
            // -- copy parent
            spellData.SetParent(Parent);
            // -- copy overriding data
            spellData.m_OverridingData = m_OverridingData;

            return spellData;
        }

        public void SetParent(string parent)
        {
            m_Parent = parent;
        }

        public void SetCurrentTargetId(ulong? targetId)
        {
            m_CurrentTargetId = targetId;
        }

        public override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_Force.SetLevel(level);

            for (int i = 0; i < OnHit.Count; i++)
            {
                var effect = OnHit[i];
                effect.SetLevel(level);
                OnHit[i] = effect;
            }

            for (int i = 0; i < m_SpellRequirements.Count; i++)
            {
                var effect = m_SpellRequirements[i];
                effect.SetLevel(level);
                m_SpellRequirements[i] = effect;
            }

            for (int i = 0; i < EnemyStateEffects.Count; i++)
            {
                var effect = EnemyStateEffects[i];
                effect.SetLevel(level);
                EnemyStateEffects[i] = effect;
            }

            for (int i = 0; i < AllyStateEffects.Count; i++)
            {
                var effect = AllyStateEffects[i];
                effect.SetLevel(level);
                AllyStateEffects[i] = effect;
            }
        }

        #endregion


        #region Scaling & Override

        protected float GetScaledValue(ESpellProperty property, float value)
        {
            // check if there is a value overriding this
            if (TryGetOverridingData(property, out float overridingValue))
            {
                return overridingValue;
            }

            if (!m_SpellsScalingLevel.Any(spell => spell.Property == property))
                return value;

            return m_SpellsScalingLevel.FirstOrDefault(spell => spell.Property == property).Get(value, m_Level);
        }

        public virtual void AddOverridingData(List<SOverridingData> overridingData, int level = 0)
        {
            foreach (var data in overridingData)
            {
                AddOverridingData(data, level);
            }
        }

        public virtual void AddOverridingData(SOverridingData overridingData, int level)
        {
            if (m_OverridingData == null)
                m_OverridingData = new();

            if (CheckSpecialOverridingData(overridingData))
                return;

            m_OverridingData.Add((overridingData.Property, overridingData.Get(GetFloat(overridingData.Property), level)));
        }

        public virtual bool CheckSpecialOverridingData(SOverridingData overridingData) 
        { 
            switch (overridingData.Property)
            {
                // -----------------------------------------------------------------------
                // Damage
                case ESpellProperty.Damage:
                    OverrideDamage(overridingData);
                    return true;

                case ESpellProperty.ExecutionDamage:
                    OverrideDamage(overridingData);
                    return true;

                // -----------------------------------------------------------------------
                // Target & Position
                case ESpellProperty.SpellTarget:
                    if (! Enum.TryParse(overridingData.Value, out ESpellTarget spellTarget))
                    {
                        ErrorHandler.Warning("Trying to override spell target of " + Name + " with value " + overridingData.Value + " - but this is not recognized as ESpellTarget");
                        return true;    // return "true" because it was recognized as "Special case" anyway
                    }
                    SpellTarget = spellTarget;
                    return true;

                // -----------------------------------------------------------------------
                // MFollowing Properties
                case ESpellProperty.IsFollowing:
                    if (!bool.TryParse(overridingData.Value, out bool isFollowing))
                    {
                        ErrorHandler.Warning("Trying to override spell target of " + Name + " with value " + overridingData.Value + " - but this is not recognized as boolean");
                        return true;
                    }
                    IsFollowing = isFollowing;
                    return true;

                case ESpellProperty.MFollowing_TargetToFollow:
                case ESpellProperty.MFollowing_TargetOffset:
                case ESpellProperty.MFollowing_FollowingSpeed:
                    // activate "MFollowing" Module if not done already
                    IsFollowing = true;
                    MFollowing.Override(overridingData.Property, overridingData.Value);
                    return true;

                // -----------------------------------------------------------------------
                // SpawnPosition Properties
                case ESpellProperty.OverridesSpawnPosition:
                    if (!bool.TryParse(overridingData.Value, out bool overrideSpawnPosition))
                    {
                        ErrorHandler.Warning("Trying to override spell target of " + Name + " with value " + overridingData.Value + " - but this is not recognized as boolean");
                        return true;
                    }
                    OverridesSpawnPosition = overrideSpawnPosition;
                    return true;

                case ESpellProperty.MSpawnPosition_SpawnTarget:
                case ESpellProperty.MSpawnPosition_SpawnLocation:
                case ESpellProperty.MSpawnPosition_BodyPart:
                case ESpellProperty.MSpawnPosition_IsFollowing:
                case ESpellProperty.MSpawnPosition_Offset:
                    // activate "SpawnPosition" Module if not done already
                    OverridesSpawnPosition = true;
                    SpawnPosition.Override(overridingData.Property, overridingData.Value);
                    return true;

                // -----------------------------------------------------------------------
                default:
                    return false;
            }
        }

        protected bool TryGetOverridingData(ESpellProperty property, out float overridingValue)
        {
            overridingValue = 0f;

            if (m_OverridingData.IsNullOrEmpty())
                return false;

            // check if there is a value overriding this
            if (m_OverridingData.Any(overridingData => overridingData.Property == property))
            {
                overridingValue = m_OverridingData.FirstOrDefault(overridingData => overridingData.Property == property).Value;
                return true;
            }

            return false;
        }

        void OverrideDamage(SOverridingData overridingData)
        {
            if (Damages.IsNullOrEmpty())
            {
                ErrorHandler.Warning("Trying to override damage but spell " + Name + " has no damage");
                return;
            }

            switch (overridingData.Property)
            {
                case ESpellProperty.Damage:
                    for (int i = 0; i < Damages.Count(); i++)
                    {
                        Damages[i].AddBonusValue(overridingData.Get(Damages[i].BaseValue, m_Level));
                    }
                    break;

                case ESpellProperty.ExecutionDamage:
                    // no execution damage : add it
                    if (! Damages.Any(t => t.HitCategory == EHitCategory.Execution))
                    {
                        Damages.Add(new SDamage(overridingData.Get(0, m_Level), EDamageCategory.Physical, EHitCategory.Execution));
                        return;
                    }

                    // execution damage exists : override the base value
                    for (int i = 0; i < Damages.Count(); i++)
                    {
                        if (Damages[i].HitCategory == EHitCategory.Execution)
                            Damages[i].AddBonusValue(overridingData.Get(Damages[i].BaseValue, m_Level));
                    }
                    break;

                default:
                    ErrorHandler.Warning("Trying to override damage but property is : " + overridingData.Property);
                    return;
            }
        }

        #endregion


        #region Public Dependent Accessors

        public void ForceAutoTarget()
        {
            switch (SpellTarget)
            {
                case (ESpellTarget.None):
                case (ESpellTarget.Free):
                case (ESpellTarget.EnemyZone):
                    SpellTarget = ESpellTarget.FirstEnemy;
                    break;

                case (ESpellTarget.AllyZone):
                    SpellTarget = ESpellTarget.FirstAlly;
                    break;
            }
        }

        #endregion
    }
}