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
using Menu.Common.Infos;

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

        [SerializeField] protected string m_Description = "";
        [SerializeField] protected List<SDescriptionVariable>   m_DescriptionVariables = new List<SDescriptionVariable>();

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
        public ESpellTarget                 SpellTarget         = ESpellTarget.EnemyZone;
        [Description("Type of targetting for the spell")]
        public ESpellEvent                  LockTargetAt        = ESpellEvent.OnCast;
        [Description("Maximum number of target that this spell can hit")]
        public int                          MaxHit              = 1;
        [Description("Energy gained when this spell hits his target")]
        public int                          EnergyGain          = 10;
        [Description("Request amount on energy to be able to cast this spell")]
        public int                          EnergyCost          = 0;
        [Description("Damage of the spell")]
        [SerializeField] public int         m_Damage            = 0;
        [Description("Heals provided to the target")]
        [SerializeField] public int         m_Heal              = 0;
        [Description("Percentage of damages healed on hit")]
        [SerializeField] protected float    m_LifeSteal         = 0f;
        [Description("Max distance of the spell")]
        public float                        Distance            = -1f;
        [Description("List of properties that are overriten on the <OnHit> spells")]
        public List<ESpellProperty>         OverrideOnHitProperties;
        [Description("Duration of the spell")]
        [SerializeField] public float       m_Duration          = 0f;
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
        public ESpell Spell => (ESpell)Id;

        // ===========================================================================
        // Level Dependent Members
        public virtual List<ESpellElement> SpellElements => m_SpellElements;
        public virtual float Cooldown           => Mathf.Max(Mathf.Round(100f * m_Cooldown / GetSpellLevelFactor(ESpellProperty.Cooldowns)) / 100f, 0f);
        public virtual int Damage               => (int)Math.Round(m_Damage * GetSpellLevelFactor(ESpellProperty.Damages));
        public virtual int Heal                 => (int)Math.Round(m_Heal * GetSpellLevelFactor(ESpellProperty.Heal));
        public virtual float LifeSteal          => m_LifeSteal * GetSpellLevelFactor(ESpellProperty.LifeSteal);
        public virtual float Duration           => m_Duration * GetSpellLevelFactor(ESpellProperty.Duration);

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

            while (delay > 0f)
            {
                delay -= Time.deltaTime;
                yield return null;
            }

            if (GameManager.IsGameOver)
                yield break;

            Cast(clientId, target, position, rotation, recalculateTarget: false);
        }

        /// <summary>
        /// Cast a the spell by instantiating the prefab, initializing it and spawning in the network
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="target"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="recalculateTarget"></param>
        public virtual void Cast(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default, bool recalculateTarget = true, bool recalculatePosition = true)
        {
            ErrorHandler.Log("Casting spell : " + Name, ELogTag.Spells);

            // Play SoundEffect
            GameManager.Instance.PlaySoundClientRPC(Name, ESpellEvent.OnCast);

            // recalculate target if required
            if (recalculateTarget)
                CalculateTarget(ref target, clientId);

            if (recalculatePosition)
                RecalculatePosition(ref position, target, clientId);

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
        }

        /// <summary>
        /// Spawn the prefabs that are displayed when the spell is casted
        /// </summary>
        /// <returns></returns>
        /// 
        public virtual List<GameObject> SpawnOnCastPrefabs(Vector3 target)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            // spawn on cast particles
            foreach (var prefab in OnCastPrefabs)
            {
                GameObject go = GameObject.Instantiate(prefab);
                Finder.FindComponent<OnCastAoe>(go).Initialize(target, Size, Delay);
                gameObjects.Add(go);
            }

            return gameObjects;
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

        public virtual void CalculateTarget(ref Vector3 target, ulong clientId) 
        {
            if (!IsAutoTarget)
                return;

            switch (SpellTarget)
            {
                case ESpellTarget.Self:
                    target = GameManager.Instance.GetPlayer(clientId).transform.position;
                    break;

                case ESpellTarget.FirstAlly:
                    target = GameManager.Instance.GetFirstAlly(GameManager.Instance.GetPlayer(clientId).Team, clientId).transform.position;
                    break;

                case ESpellTarget.FirstEnemy:
                    target = GameManager.Instance.GetFirstEnemy(GameManager.Instance.GetPlayer(clientId).Team).transform.position;
                    break;

                case ESpellTarget.AllyZoneCenter:
                case ESpellTarget.EnemyZoneCenter:
                    target = new Vector3(GetTargettableArea(GameManager.Instance.GetPlayer(clientId).Team).position.x, target.y, target.z);
                    break;

                case ESpellTarget.AllyZoneStart:
                case ESpellTarget.EnemyZoneStart:
                    var controller = GameManager.Instance.GetPlayer(clientId);
                    var centerPos = GetTargettableArea(controller.Team).position.x;

                    // direction usless ??
                    int direction = ArenaManager.GetAreaMovementDirection(controller.Team, SpellTarget == ESpellTarget.EnemyZoneStart);
                    target = new Vector3(centerPos - direction * ArenaManager.Instance.TargettableAreaSize / 2, target.y, target.z);
                    break;

                case ESpellTarget.Mirror:
                    target = new Vector4(-GameManager.Instance.GetPlayer(clientId).transform.position.x, target.y, 0f);
                    break;

                case ESpellTarget.Fixed:
                    target = new Vector4(GameManager.Instance.GetPlayer(clientId).transform.position.x + 7f, target.y, 0f);
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

        protected virtual void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId) { }


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
        /// Get Description info of the StateEffect
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescription()
        {
            List<string> values = new List<string>();
            var infos = GetInfos();

            foreach (SDescriptionVariable descriptionVariable in m_DescriptionVariables)
            {
                if (Enum.TryParse(descriptionVariable.Name, out EStateEffect stateEffect))
                {
                    values.Add(TextHandler.FormatStateEffectIcon(descriptionVariable.Name, descriptionVariable.WithIcon));
                }
                else if (infos.ContainsKey(descriptionVariable.Name))
                {
                    string value = infos[descriptionVariable.Name].ToString();
                    if (float.TryParse(value, out float floatValue)) 
                        value = SpellInfoRowUI.FormatValue(floatValue, SpellInfoRowUI.CheckIsPercentageValue(descriptionVariable.Name));
                    
                    string iconTag = descriptionVariable.WithIcon ? $" <sprite name=\"{"Ic_" + descriptionVariable.Name}\">" : "";
                    values.Add($"<b>{value}</b>{iconTag}");
                }
                else if (Enum.TryParse(descriptionVariable.Name, out ESpellProperty property))
                {
                    if (! TryGetProperty(property, out object value))
                    {
                        ErrorHandler.Error("Unable to find property " + property + " in spell " + Name);
                        values.Add("<b>UNDEFINED</b>");
                        continue;
                    }
                    values.Add($"<b>{value}</b>");
                }
                else
                {
                    ErrorHandler.Error("Unable to find property " + descriptionVariable.Name + " in info dict of spell " + Name);
                    values.Add("<b>UNDEFINED</b>");
                }
            }

            return string.Format(m_Description, values.ToArray());
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
            if (IsAutoTarget)
                return;

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
                return SpellTarget != ESpellTarget.None
                    && SpellTarget != ESpellTarget.EnemyZone
                    && SpellTarget != ESpellTarget.AllyZone
                    && SpellTarget != ESpellTarget.Free;
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