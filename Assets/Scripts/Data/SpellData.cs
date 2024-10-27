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

namespace Data
{
    [Serializable]
    public struct SDescriptionVariable
    {
        public string Name;
        public bool WithIcon;
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
        [Description("List of Element catagories of the spell")]
        [SerializeField] protected List<ESpellElement> m_SpellElements;
        [Description("Is this spell linked to a specific character")]
        public bool                 Linked;

        [Header("Prefabs")]
        [Description("Prefab of the spell that will be instantiated when the spell is cast")]
        public GameObject           Graphics;

        [Description("List of all Effects appening when the targets")]
        public List<SPrefabSpawn>   SpellEventActions;

        [Description("Preview of the spell target on the ground displayed before the cast of the spell")]
        public GameObject           Preview;
        [Description("Particles displayed during the animation")]
        public List<SPrefabSpawn>   OnAnimation;
        [Description("Particles displayed when the cast is done")]
        public List<GameObject>     OnCastPrefabs;
        [Description("Prefab of the spell when it hits a target")]
        public List<SpellData>      OnHit;

        [Description("Where does the OnHit spawns")]
        public ESpellSpawn OnHitSpellSpawn = ESpellSpawn.Ground;

        [Header("Sound Effects")]
        public AudioClip AnimationSoundFX;
        public AudioClip CastSoundFX;
        public AudioClip PermanantSoundFX;
        public AudioClip OnHitSoundFX;
        public AudioClip OnEndSoundFX;

        [Header("Stats")]
        [Description("Type of targetting for the spell")]
        public ESpellTarget                 SpellTarget         = ESpellTarget.FirstEnemy;
        [Description("Type of targetting for the spell")]
        public ESpellEvent                  LockTargetAt        = ESpellEvent.OnCast;
        [Description("Is the spell effect applied when NOT hitting the target ?")]
        public bool                         ApplyIfNotHitting   = false;
        [Description("Maximum number of target that this spell can hit")]
        public int                          MaxHit              = 1;
        [Description("Energy gained when this spell hits his target")]
        public int                          EnergyGain          = 10;
        [Description("Request amount on energy to be able to cast this spell")]
        public int                          EnergyCost          = 0;
        [SerializeField, Description("Damage of the spell")]
        public int                          m_Damage            = 0;
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
        // Dependent Members
        public virtual ESpellType SpellType => ESpellType.InstantSpell;
        public float Size => m_Size >= 0 ? m_Size * Settings.SpellSizeFactor : ArenaManager.Instance.TargettableAreaSize;
        protected override Type m_EnumType => typeof(ESpell);
        public ESpell Spell => Id == null ? ESpell.None : (ESpell)Id;

        // ===========================================================================
        // Level Dependent Members
        public virtual List<ESpellElement> SpellElements => m_SpellElements;
        public virtual float Cooldown           => Mathf.Max(Mathf.Round(100f * m_Cooldown / GetSpellLevelFactor(ESpellProperty.Cooldowns)) / 100f, 0f);
        public virtual int Damage               => (int)Math.Round(m_Damage * GetSpellLevelFactor(ESpellProperty.Damages));
        public virtual int Heal                 => (int)Math.Round(m_Heal * GetSpellLevelFactor(ESpellProperty.Heal));
        public virtual int Shield               => (int)Math.Round(m_Shield * GetSpellLevelFactor(ESpellProperty.Shield));
        public virtual float LifeSteal          => m_LifeSteal * GetSpellLevelFactor(ESpellProperty.LifeSteal);
        public virtual float Duration           => m_Duration * GetSpellLevelFactor(ESpellProperty.Duration);
        /// <summary> is the "IsCasting" over once the spell has been casted (before delay) ? </summary>
        public virtual bool IsCompletedOnCast   => true;

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
        public IEnumerator CastDelay(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default, float? delay = null, bool recalculateTarget = true)
        {
            if (delay == null)
                delay = Delay;

            // recalculate target depending on spell type
            if (recalculateTarget)
                CalculateTarget(ref target, clientId);

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
            bool recalculateOnCast = LockTargetAt == ESpellEvent.OnSpawn;
            Cast(clientId, target, position, rotation, recalculateTarget: recalculateOnCast && recalculateTarget);
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
            GameObject spellGO = GameObject.Instantiate(GetSpellPrefab(), position, rotation);

            // spawn in network
            Finder.FindComponent<NetworkObject>(spellGO).SpawnWithOwnership(clientId);

            // reparent if any
            Transform parent = FindParent(clientId);
            if (parent != null)
                spellGO.transform.SetParent(FindParent(clientId));

            // initialize the spell
            var spell = Finder.FindComponent<Spell>(spellGO);
            spell.Initialize(clientId, target, Name, m_Level);

            // backpropagate the spell intialization to the client (for the preview)
            spell.InitializeClientRpc(clientId, target, Name, m_Level);

            // call event that spell spawned
            GameManager.Instance.GetPlayer(clientId).SpellHandler.CallSpellEvent(Name, ESpellEvent.OnSpawn);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="position"></param>
        /// 
        /// <param name="rotation"></param>
        public void SpawnOnHitPrefab(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default)
        {
            if (OnHit == null || OnHit.Count == 0)
                return;

            foreach(SpellData spellData in OnHit)
            {
                // setup spell data to level of this spell
                var onHitSpellData = spellData.Clone(m_Level);
                onHitSpellData.Override(this);
                onHitSpellData.OverrideSpellSpawn(OnHitSpellSpawn);
                onHitSpellData.Cast(clientId, target, position, rotation, false);
            }   
        }

        /// <summary>
        /// Display the preview of the spell on the ground where the player is aiming
        /// </summary>
        public virtual void SpellPreview(Controller controller, Transform parent = default, Vector3 offset = default)
        {
            if (Preview == null)
                return;

            if (parent == default)
                parent = controller.SpellHandler.SpellSpawn;

            // instantiate the gameobject of the preview
            var preview = GameObject.Instantiate(Preview, parent);
            preview.transform.localPosition += offset;

            // get the component of the preview and initialize it
            var component = Finder.FindComponent<SpellPreview>(preview);
            component.Initialize(GetTargettableArea(controller.Team), Distance, Size);
        }

        #endregion


        #region Spell GFX

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

        protected virtual GameObject GetSpellPrefab()
        {
            return SpellLoader.GetSpellPrefab(Name, SpellType);
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

        public virtual void CalculateTarget(ref Vector3 target, ulong clientId) 
        {
            if (!IsAutoTarget)
                return;

            Controller controller = GameManager.Instance.GetPlayer(clientId);
            int direction;
            switch (SpellTarget)
            {
                case ESpellTarget.Self:
                    target = controller.transform.position;
                    break;

                case ESpellTarget.FirstAlly:
                    target = GameManager.Instance.GetFirstAlly(controller.Team, clientId).transform.position;
                    break;

                case ESpellTarget.FirstEnemy:
                    target = GameManager.Instance.GetFirstEnemy(controller.Team).transform.position;
                    break;

                case ESpellTarget.AllyZoneCenter:
                case ESpellTarget.EnemyZoneCenter:
                    target = new Vector3(GetTargettableArea(controller.Team).position.x, target.y, target.z);
                    break;

                case ESpellTarget.AllyZoneStart:
                case ESpellTarget.EnemyZoneStart:
                    var centerPos = GetTargettableArea(controller.Team).position.x;
                    direction = ArenaManager.GetAreaMovementDirection(controller.Team, SpellTarget == ESpellTarget.EnemyZoneStart);
                    target = new Vector3(centerPos - direction * ArenaManager.Instance.TargettableAreaSize / 2, target.y, target.z);
                    break;

                case ESpellTarget.Mirror:
                    target = new Vector3(-controller.transform.position.x, target.y, 0f);
                    break;

                case ESpellTarget.Fixed:
                    direction = ArenaManager.GetAreaMovementDirection(controller.Team, true);
                    target = new Vector3(controller.transform.position.x + direction * Settings.SpellFixedDistance, target.y, 0f);
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + SpellTarget);
                    break;
            }

            ClampTargetX(ref target, clientId);
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


        #region Position

        public virtual void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId) { }
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
            if (Heal > 0)
                infosDict.Add("Heal", Heal);
            if (Duration > 0)
                infosDict.Add("Duration", Duration);
            if (m_Size > 0 && m_Size != 1)
                infosDict.Add("Size", m_Size);
            
            infosDict.Add("Cooldown", Cooldown);
            infosDict.Add("CastDuration", AnimationTimer);

            if (Distance > 0)
                infosDict.Add("Distance", Distance);

            // add state effects
            AddStateEffectInfos(ref infosDict);
            
            // add on hit infos if has any
            AddOnHitInfos(ref infosDict);
            
            return infosDict;
        }

        /// <summary>
        /// Convert a description variable into a string implemented into the description
        /// </summary>
        /// <returns></returns>
        public override string ConvertDescriptionVariable(SDescriptionVariable descriptionVariable, Dictionary<string, object> infos = default)
        {
            if (Enum.TryParse(descriptionVariable.Name, out ESpellProperty property))
            {
                if (!TryGetProperty(property, out object value))
                {
                    ErrorHandler.Error("Unable to find property " + property + " in spell " + Name);
                    return "<b>UNDEFINED</b>";
                }

                if (float.TryParse(value.ToString(), out float floatValue))
                    value = TextHandler.FormatPropertyValue(floatValue, descriptionVariable.Name);

                return $"<b>{value}</b>";
            }

            return base.ConvertDescriptionVariable(descriptionVariable, infos);
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
            string[] keysToIgnore = new string[] { "Type", "Target", "Cooldown", "CastDuration", "Distance", "EnergyCost" };        // keys to ignore as overwrite  
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

        public string GetTypeInfo()
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

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            for (int i = 0; i < OnHit.Count; i++)
            {
                OnHit[i] = OnHit[i].Clone(level);
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
            || SpellTarget == ESpellTarget.Fixed
            || SpellTarget == ESpellTarget.Mirror;
            

        public bool IsAllyTarget => SpellTarget == ESpellTarget.FirstAlly
            || SpellTarget == ESpellTarget.Self
            || SpellTarget == ESpellTarget.AllyZone
            || SpellTarget == ESpellTarget.AllyZoneStart
            || SpellTarget == ESpellTarget.AllyZoneCenter;

        #endregion

    }
}