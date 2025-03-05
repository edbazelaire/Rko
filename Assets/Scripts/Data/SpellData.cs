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
using Unity.Mathematics;
using Game.NetworkStructures;

namespace Data
{
    [Serializable]
    public struct SDescriptionVariable
    {
        public string Name;
        public bool WithIcon;

        public SDescriptionVariable(string name, bool withIcon = true)
        {
            Name = name;
            WithIcon = withIcon;
        }
    }

    [Serializable]
    public struct SSpellPropertyScaling
    {
        public ESpellProperty Property;
        public float Value;

        public SSpellPropertyScaling(ESpellProperty prop, float value = 0f)
        {
            Property = prop;
            Value = value;
        }
    }


    [CreateAssetMenu(fileName = "Spell", menuName = "Game/Spells/Default")]
    public class SpellData : CollectableData
    {
        #region Members

        // ===========================================================================
        // Serialized Data
        [SerializeField, Description("List of Element catagories of the spell")]
        protected List<ESpellElement>   m_SpellElements;
        [Description("Is this spell linked to a specific character")]
        public bool                     Linked;

        [Header("Prefabs")]
        [Description("Prefab of the spell that will be instantiated when the spell is cast")]
        public GameObject               Graphics;

        [Description("List of all Effects appening when the targets")]
        public List<SpellPrefabSpawn>   SpellEventActions;

        [Description("Prefab of the spell when it hits a target")]
        public List<SpellData>          OnHit;

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
        public ESpellTarget                 SpellTarget             = ESpellTarget.FirstEnemy;
        [SerializeField, Tooltip("Clamp target position between arena bounds")]
        protected bool m_ClampTargetPos                             = true;
        [Description("Target offset X/Y")]
        public SOffset                      TargetOffset            = new SOffset(0, 0);
        [Description("Type of targetting for the spell")]
        public ESpellEvent                  LockTarget              = ESpellEvent.OnCast;
        [Description("Is the spell effect applied when NOT hitting the target ?")]
        public bool                         ApplyIfNotHitting       = false;
        
        [Header("Requirements")]
        [SerializeField, Description("Is the spell effect applied when NOT hitting the target ?")]
        protected List<SpellRequirements>   m_SpellRequirements    = new List<SpellRequirements>();

        [Header("Stats")]
        [Description("Maximum number of target that this spell can hit")]
        public int                          MaxHit                  = 1;
        [Description("Energy gained when this spell hits his target")]
        public int                          EnergyGain              = 10;
        [Description("Request amount on energy to be able to cast this spell")]
        public int                          EnergyCost          = 0;
        [SerializeField, Description("Damage of the spell")]
        public int                          m_Damage            = 0;
        [SerializeField, Description("Execution damage of the spell (growing with missing life)")]
        public int                          m_ExecutionDamages  = 0;
        [SerializeField, Description("Heals provided to the target")]
        public int                          m_Heal              = 0;
        [SerializeField, Description("Quantity of (permanant) shield provided to the target")] 
        public int                          m_Shield            = 0;
        [SerializeField, Description("Percentage of damages healed on hit")]
        protected float                     m_LifeSteal         = 0f;
        [Description("Max distance of the spell")]
        public float                        Distance            = -1f;
        [Description("List of properties that are overriten on the <OnHit> spells")]
        public List<ESpellProperty>         OverrideOnHitProperties;
        [SerializeField, Description("Duration of the spell")]
        public float                        m_Duration          = 0f;
        [Description("Delay of the spell to be instantiated after cast")]
        public float                        Delay               = 0f;
        [SerializeField, Description("Force applied on hitting the target")]
        protected SForce                    m_Force             = default;

        [Header("Scaling")]
        [SerializeField] protected List<SSpellPropertyScaling> m_SpellsScalingLevel = new() { 
            new SSpellPropertyScaling(ESpellProperty.Damages, 0.1f), 
            new SSpellPropertyScaling(ESpellProperty.Heal, 0.1f), 
            new SSpellPropertyScaling(ESpellProperty.Cooldowns, 0.05f), 
        };

        [Header("Collision")]
        [Description("Size of the spell (and hitbox)")]
        [SerializeField] public float   m_Size = 1f;
        [Description("Does the spell get trigger on touching a player")]
        public bool                     TriggerPlayer = true;

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
        [Description("Can the animation be cancelled ?")]
        public bool IsCancellable = false;
        [Description("Time for the animation to take from start to begin (in seconds)")]
        public float AnimationTimer;
        [Description("Cooldown to be able to re-use that ability")]
        [SerializeField] protected float m_Cooldown;

        // ===========================================================================
        string m_Parent;

        // ===========================================================================
        // Dependent Members
        public virtual string Parent => m_Parent.IsNullOrEmpty() ? Name : m_Parent;
        public virtual List<ESpellElement> SpellElements => m_SpellElements;
        public virtual  List<SpellRequirements> SpellRequirements => m_SpellRequirements;
        public virtual ESpellType   SpellType   => ESpellType.InstantSpell;
        public float                BaseSize    => m_Size;
        public float                Size        => m_Size >= 0 ? m_Size * Settings.SpellSizeFactor : ArenaManager.Instance.TargettableAreaSize;
        protected override Type     m_EnumType  => typeof(ESpell);
        public ESpell               Spell       => Id == null ? ESpell.None : (ESpell)Id;

        // ===========================================================================
        // Level Dependent Members
        public virtual float Cooldown           => Mathf.Max(Mathf.Round(100f * m_Cooldown / GetSpellLevelFactor(ESpellProperty.Cooldowns)) / 100f, 0f);
        public virtual int Damage               => (int)Math.Round(m_Damage * GetSpellLevelFactor(ESpellProperty.Damages));
        public virtual int ExecutionDamages     => (int)Math.Round(m_ExecutionDamages * GetSpellLevelFactor(ESpellProperty.ExecutionDamages));
        public virtual int Heal                 => (int)Math.Round(m_Heal * GetSpellLevelFactor(ESpellProperty.Heal));
        public virtual int Shield               => (int)Math.Round(m_Shield * GetSpellLevelFactor(ESpellProperty.Shield));
        public virtual float LifeSteal          => m_LifeSteal * GetSpellLevelFactor(ESpellProperty.LifeSteal);
        public virtual float Duration           => m_Duration * GetSpellLevelFactor(ESpellProperty.Duration);
        public virtual SForce Force             => m_Force;

        /// <summary> is the "IsCasting" over once the spell has been casted (before delay) ? </summary>
        public virtual bool IsCompletedOnCast   => true;
        public virtual ESpellCategory SpellCategory => ESpellCategory.Direct;

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
                CalculateTarget(ref target, clientId);

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
                CalculateTarget(ref target, clientId);

            if (recalculatePosition)
                RecalculatePosition(ref position, target, clientId);

            if (recalculateRotation)
                RecalculateRotation(ref rotation);

            // instantiate the prefab of the spell
            NetworkObject spellGO = PoolManager.Pool(GetSpellPrefab(), clientId, position, rotation);

            // reparent if any
            Transform parent = FindParent(clientId);
            if (parent != null)
                spellGO.transform.SetParent(FindParent(clientId));

            // initialize the spell
            var spell = Finder.FindComponent<Spell>(spellGO.gameObject);
            spell.Initialize(clientId, target, Name, m_Level, m_Parent);

            // backpropagate the spell intialization to the client (for the preview)
            spell.InitializeClientRpc(clientId, new Vector2Short(target), Name, (byte)m_Level);

            // call event that spell spawned
            CallSpellEvent(GameManager.Instance.GetPlayer(clientId), ESpellEvent.OnSpawn, target);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void SpawnOnHitPrefab(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default)
        {
            if (OnHit == null || OnHit.Count == 0)
                return;

            var controller = GameManager.Instance.GetPlayer(clientId);

            foreach(SpellData spellData in OnHit)
            {
                // setup spell data to level of this spell
                var onHitSpellData = spellData.Clone(m_Level);
                onHitSpellData.SetParent(Parent);

                onHitSpellData.Override(this);
                onHitSpellData.OverrideSpellSpawn(OnHitSpellSpawn);
                controller.StartCoroutine(onHitSpellData.CastDelay(clientId, target, position, rotation, recalculateTarget: false, recalculatePosition: false));

                // call graphics event
                controller.SpellHandler.CallSpellEvent(onHitSpellData.Name, ESpellEvent.OnStartCast, targetPosition: target);
            }
        }

        #endregion


        #region Spell GFX

        void CallSpellEvent(Controller controller, ESpellEvent spellEvent, Vector3 target)
        {
            // spawn SubSpell - SpellGFX
            controller.SpellHandler.CallSpellEvent(Name, spellEvent, targetPosition: target);
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


        #region Ending

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var onHit in OnHit)
                Destroy(onHit);
        }

        #endregion


        #region Overriders 

        public void Override(SpellData overridingData)
        {
            if (overridingData.OverrideOnHitProperties.Count == 0)
                return;

            ErrorHandler.Log("Overriding data of " + Name + " with " + overridingData.Name, ELogTag.Spells);

            foreach (ESpellProperty spellProperty in overridingData.OverrideOnHitProperties)
            {
                ErrorHandler.Log("      + " + spellProperty + " : ", ELogTag.Spells);
                ErrorHandler.Log("          - FROM : " + GetProperty(spellProperty), ELogTag.Spells);

                SetProperty(spellProperty, overridingData.GetProperty(spellProperty));

                ErrorHandler.Log("          - TO : " + GetProperty(spellProperty), ELogTag.Spells);
            }
        }

        public virtual void OverrideSpellSpawn(ESpellSpawn spellSpawn) { }

        #endregion


        #region Spell Helpers

        protected virtual NetworkObject GetSpellPrefab()
        {
            return SpellLoader.GetSpellPrefab(Name, SpellType).GetComponent<NetworkObject>();
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

        public virtual Controller GetTargetController(ulong casterId)
        {
            Controller controller = GameManager.Instance.GetPlayer(casterId);

            switch (SpellTarget)
            {
                case ESpellTarget.Self:
                    return controller;

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(controller.Team, casterId);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(controller.Team);

                default:
                    ErrorHandler.Warning("No controller found for target of type " + SpellTarget);
                    return null;
            }
        }

        public virtual void CalculateTarget(ref Vector3 target, ulong casterId) 
        {
            if (!IsAutoTarget)
                return;

            Controller controller = GameManager.Instance.GetPlayer(casterId);
            int direction = ArenaManager.GetAreaMovementDirection(controller.Team, IsEnemyTarget);

            switch (SpellTarget)
            {
                case ESpellTarget.Self:
                    target.x = controller.transform.position.x;
                    break;

                case ESpellTarget.FirstAlly:
                    target.x = GameManager.Instance.GetFirstAlly(controller.Team, casterId).transform.position.x;
                    break;

                case ESpellTarget.FirstEnemy:
                    target.x = GameManager.Instance.GetFirstEnemy(controller.Team).transform.position.x;
                    break;

                case ESpellTarget.AllyZoneCenter:
                case ESpellTarget.EnemyZoneCenter:
                    target.x = GetTargettableArea(controller.Team).position.x;
                    break;

                case ESpellTarget.AllyZoneStart:
                case ESpellTarget.EnemyZoneStart:
                    var centerPos = GetTargettableArea(controller.Team).position.x;
                    target.x = centerPos - direction * ArenaManager.Instance.TargettableAreaSize / 2;
                    break;

                case ESpellTarget.AllyZoneEnd:
                case ESpellTarget.EnemyZoneEnd:
                    target.x = GetTargettableArea(controller.Team).position.x + direction * ArenaManager.Instance.TargettableAreaSize / 2;
                    break;

                case ESpellTarget.Mirror:
                    target.x = -controller.transform.position.x;
                    break;

                case ESpellTarget.Fixed:
                    direction = ArenaManager.GetAreaMovementDirection(controller.Team, true);
                    target.x = controller.transform.position.x + direction * Settings.SpellFixedDistance;
                    break;

                default:
                    ErrorHandler.Error("("+ Name +") - Unhandled case : " + SpellTarget);
                    break;
            }

            // APPLY OFFSET
            target.x += direction * TargetOffset.X;
            target.y += TargetOffset.Y;

            // CLAMP target in between available positions
            if (m_ClampTargetPos && SpellTarget != ESpellTarget.Self)
                ClampTargetX(ref target, casterId);
        }

        protected virtual void ClampTargetX(ref Vector3 target, ulong clientId) 
        {
            // clamp target between min/max xPos of the target zone
            var zoneCenter = GetTargettableArea(GameManager.Instance.GetPlayer(clientId).Team).position.x;
            target.x = Mathf.Clamp(target.x, zoneCenter - ArenaManager.Instance.TargettableAreaSize / 2, zoneCenter + ArenaManager.Instance.TargettableAreaSize / 2);
        }

        public Transform GetTargettableArea(int team)
        {
            if (IsEnemyTarget)
                return ArenaManager.GetTargettableArea(team, true);

            else if (IsAllyTarget)
                return ArenaManager.GetTargettableArea(team, false);
            else
                return ArenaManager.Instance.Arena.transform;
        }

        /// <summary>
        /// Method for children to change parent spawning if need be
        /// </summary>
        /// <returns></returns>
        protected virtual Transform FindParent(ulong clientId)
        {
            return null;
        }

        #endregion


        #region Position & Rotation

        public virtual void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId) 
        {
            position = GameManager.Instance.GetPlayer(clientId).GFXHandler.GetSpellSpawn().position;
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
                case ESpellProperty.Damages:
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

        public override Dictionary<string, object> GetInfos()
        {
            var infosDict = base.GetInfos();
            
            infosDict.Add("Type", GetTypeInfo());
            infosDict.Add("Target", GetTargetTypeInfo());

            if (EnergyGain > 0)
                infosDict.Add("Energy", EnergyGain);
            if (EnergyCost > 0)
                infosDict.Add("EnergyCost", EnergyCost);
            if (Damage > 0)
                infosDict.Add("Damages", Damage);
            if (ExecutionDamages > 0)
                infosDict.Add("ExecutionDamages", ExecutionDamages);
            if (Heal > 0)
                infosDict.Add("Heal", Heal);
            if (Duration > 0)
                infosDict.Add("Duration", Duration);
            if (m_Size > 0 && m_Size != 1)
                infosDict.Add("Size", m_Size);
            
            infosDict.Add("Cooldown",       Cooldown);
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
            description = TextHandler.ReplaceSubStateEffects(description, this);
            description = TextHandler.ReplaceSpellRequirements(description, this);

            return description;
        }

        /// <summary>
        /// Convert a description variable into a string implemented into the description
        /// </summary>
        /// <returns></returns>
        public override string ConvertDescriptionVariable(SDescriptionVariable descriptionVariable, Dictionary<string, object> infos = default, bool throwError = true)
        {
            string stringValue = base.ConvertDescriptionVariable(descriptionVariable, infos, false);
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
            if (OnHit.Count == 0)
                return;

            if (OnHit.Count > 1)
            {
                ErrorHandler.Warning("OnHit.Count > 1 : this case is not handled in infos description");
                return;
            }

            OnHit[0].AddAsSubSpellInfos(ref infosDict);
        }

        public void AddAsSubSpellInfos(ref Dictionary<string, object> infosDict)
        {
            string[] keysToIgnore = new string[] { "Type", "Target", "Cooldown", "CastDuration", "Distance", "EnergyCost", "Delay" };        // keys to ignore as overwrite  
            string[] keysToAdd = new string[] { "Damages", "Heal", "TickDamages", "TickHeal", "Effects" };                                // keys that are not overritten but additionned 

            var subSpellInfos = GetInfos();
            foreach (var info in subSpellInfos)
            {
                // skip some keys
                if (keysToIgnore.Contains(info.Key))
                    continue;

                // keys overriden by the base spell
                if (OverrideOnHitProperties.Any(property => property.ToString().Equals(info.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (keysToAdd.Contains(info.Key) && infosDict.ContainsKey(info.Key))
                {
                    if (info.Key == "Effects")
                    {
                        ((List<SStateEffectData>)infosDict[info.Key]).AddRange((List<SStateEffectData>)info.Value);
                        continue;
                    }

                    if (!float.TryParse(infosDict[info.Key].ToString(), out float baseValue))
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
            switch(SpellTarget)
            {
                case ESpellTarget.Fixed:
                case ESpellTarget.Mirror:
                case ESpellTarget.Self:
                    return SpellTarget.ToString();

                default:
                    return "Auto";
            }
        }

        #endregion


        #region Level management

        public new SpellData Clone(int level = 0, bool destroy = false)
        {
            return (SpellData)base.Clone(level, destroy);
        }

        public void SetParent(string parent)
        {
            m_Parent = parent;
        }

        public override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_Force.SetLevel(level);

            for (int i = 0; i < OnHit.Count; i++)
            {
                OnHit[i] = OnHit[i].Clone(level);
            }

            for (int i = 0; i < m_SpellRequirements.Count; i++)
            {
                m_SpellRequirements[i].SetLevel(level);
            }

            for (int i = 0; i < EnemyStateEffects.Count; i++)
            {
                EnemyStateEffects[i].SetLevel(level);
            }

            for (int i = 0; i < AllyStateEffects.Count; i++)
            {
                AllyStateEffects[i].SetLevel(level);
            }
        }

        protected float GetSpellLevelFactor(ESpellProperty property)
        {
            var data = m_SpellsScalingLevel.FirstOrDefault(spell => spell.Property == property);
            return (float)Math.Pow( 1 + data.Value, m_Level - 1);
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

        public bool IsAutoTarget
        {
            get
            {
                return true;
            }
        }

        public bool IsEnemyTarget => SpellTarget == ESpellTarget.FirstEnemy
            || SpellTarget == ESpellTarget.EnemyZone
            || SpellTarget == ESpellTarget.EnemyZoneStart
            || SpellTarget == ESpellTarget.EnemyZoneCenter
            || SpellTarget == ESpellTarget.EnemyZoneEnd
            || SpellTarget == ESpellTarget.Fixed
            || SpellTarget == ESpellTarget.Mirror;
            

        public bool IsAllyTarget => SpellTarget == ESpellTarget.FirstAlly
            || SpellTarget == ESpellTarget.Self
            || SpellTarget == ESpellTarget.AllyZone
            || SpellTarget == ESpellTarget.AllyZoneStart
            || SpellTarget == ESpellTarget.AllyZoneCenter
            || SpellTarget == ESpellTarget.AllyZoneEnd;

        #endregion

    }
}