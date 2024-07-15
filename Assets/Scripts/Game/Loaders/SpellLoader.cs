using Data;
using Data.GameManagement;
using Enums;
using Game.Spells;
using Inventory;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Game.Loaders
{
    public class SpellLoader : MonoBehaviour
    {
        #region Members

        static SpellLoader s_Instance;

        Dictionary<string, GameObject> m_SpellsPrefabs;
        Dictionary<ESpell, SpellData> m_Spells;
        Dictionary<string, SpellData> m_OnHitSpellData;
        Dictionary<string, StateEffect> m_StateEffects;
        Dictionary<ERune, RuneData> m_RunesData;

        public static List<ESpell> Spells => Instance.m_Spells.Keys.ToList();

        #endregion


        #region Initialization

        void Initialize()
        {
            InitializeSpellPrefabs();
            InitializeSpells();
            InitializeStateEffects();
            InitializeRuneData();

            DontDestroyOnLoad(s_Instance.gameObject);
        }

        void InitializeSpellPrefabs()
        {
            m_SpellsPrefabs = new Dictionary<string, GameObject>();

            GameObject[] spellPrefabs = AssetLoader.LoadSpellPrefabs();
            foreach (GameObject spellPrefab in spellPrefabs)
            {
                m_SpellsPrefabs.Add(spellPrefab.name, spellPrefab);
            }
        }

        void InitializeSpells()
        {
            SpellData[] spellList = LoadSpells();

            m_Spells = new Dictionary<ESpell, SpellData>();
            m_OnHitSpellData = new Dictionary<string, SpellData>();

            foreach (SpellData spell in spellList)
            {
                if (spell.name.StartsWith("_"))
                {
                    m_OnHitSpellData.Add(spell.name, spell);
                    continue;
                }

                if (spell.AnimationTimer < 0)
                    ErrorHandler.FatalError($"SpellLoader : AnimationTimer {spell.Spell} < 0");

                m_Spells.Add(spell.Spell, spell);
            }
        }

        void InitializeStateEffects()
        {
            StateEffect[] allData = LoadStateEffects();

            m_StateEffects = new Dictionary<string, StateEffect>();

            foreach (StateEffect data in allData)
            {
                m_StateEffects.Add(data.name, data);
            }
        }

        void InitializeRuneData()
        {
            RuneData[] allData = LoadRunesData();

            m_RunesData = new Dictionary<ERune, RuneData>();

            foreach (RuneData data in allData)
            {
                if (! Enum.TryParse(data.Name, out ERune rune))
                {
                    ErrorHandler.Warning(data.Name + " not found in list of runes enum : ERune - skipping");
                    continue;
                }

                m_RunesData.Add(rune, data);
            }

            // check all runes have been loaded
            foreach (ERune rune in Enum.GetValues(typeof(ERune)))
            {
                if (! m_RunesData.ContainsKey(rune))
                {
                    ErrorHandler.Warning("missing RuneData for rune " + rune);
                }
            }
        }

        SpellData[] LoadSpells()
        {
           return Resources.LoadAll<SpellData>("Data/Spells");
        }

        StateEffect[] LoadStateEffects()
        {
           return Resources.LoadAll<StateEffect>("Data/StateEffects");
        }

        RuneData[] LoadRunesData()
        {
           return Resources.LoadAll<RuneData>("Data/Runes");
        }

        #endregion


        #region Data Management Accessors

        public static SRaretyData GetRaretyData(ERarety rarety)
        {
            return CollectablesManagementData.GetRaretyData(rarety);
        }

        public static SRaretyData GetRaretyData(ESpell spell)
        {
            return CollectablesManagementData.GetRaretyData(Instance.m_Spells[spell].Rarety);
        }

        public static SLevelData GetSpellLevelData(ESpell spell)
        {
            SCollectableCloudData data = InventoryManager.GetSpellData(spell);
            if (data.Level == 0)
                InventoryManager.Unlock(ref data);
            return CollectablesManagementData.GetSpellLevelData(data.Level, Instance.m_Spells[spell].Rarety);
        }

        #endregion


        #region Static Manipulators

        /// <summary>
        /// 
        /// </summary>
        public static SpellLoader Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindFirstObjectByType<SpellLoader>();
                    if (s_Instance == null)
                        return null;
                    s_Instance.Initialize();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// Check if spell exists
        /// </summary>
        /// <param name="spellName"></param>
        /// <returns></returns>
        public static bool SpellExists(string name)
        {
            return Enum.TryParse(name, out ESpell _) || Instance.m_OnHitSpellData.ContainsKey(name);
        }

        /// <summary>
        /// Check if spell exists
        /// </summary>
        /// <param name="spellName"></param>
        /// <returns></returns>
        public static bool StateEffectExists(string name)
        {
            return Enum.TryParse(name, out EStateEffect _) || Instance.m_StateEffects.ContainsKey(name);
        }

        /// <summary>
        /// Get the specific prefab for a spell if exists, otherwise return default prefab for this type of spell
        /// </summary>
        /// <param name="spellName"></param>
        /// <param name="spellType"></param>
        /// <returns></returns>
        public static GameObject GetSpellPrefab(string spellName, ESpellType spellType)
        {
            if (spellType == ESpellType.MultiProjectiles)
                spellType = ESpellType.Projectile;

            // check for specific prefab of the spell
            if (Instance.m_SpellsPrefabs.ContainsKey(spellName))
                return Instance.m_SpellsPrefabs[spellName];

            // check that exists
            if (! Instance.m_SpellsPrefabs.ContainsKey(spellType.ToString()))
                ErrorHandler.FatalError("Unable to find default prefab for spell type " + spellType.ToString());

            // returns default prefab for spell type
            return Instance.m_SpellsPrefabs[spellType.ToString()];
        } 

        /// <summary>
        /// Get the spell data of the given spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public static SpellData GetSpellData(ESpell spell, int level = 1)
        {
            if (!Instance.m_Spells.ContainsKey(spell))
            {
                ErrorHandler.Error($"SpellLoader : Spell {spell} not found");
                return null;
            }

            return Instance.m_Spells[spell].Clone(level);
        }

        /// <summary>
        /// Get the spell data of the given spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public static SpellData GetSpellData(string spellName, int level = 1)
        {
            if (Enum.TryParse(spellName, out ESpell spell))
            {
                return GetSpellData(spell, level);
            }

            if (Instance.m_OnHitSpellData.ContainsKey(spellName))
            {
                return Instance.m_OnHitSpellData[spellName].Clone(level);
            }

            ErrorHandler.Error($"SpellLoader : Spell {spellName} not found");
            return null;
        }

        /// <summary>
        /// Get all spells of a specific rarety
        /// </summary>
        /// <param name="rarety"></param>
        /// <returns></returns>
        public static List<SpellData> GetSpellsFromRarety(ERarety rarety, bool? unlocked = null) 
        { 
            List<SpellData> spells = new List<SpellData>();
            foreach (var spellData in Instance.m_Spells.Values)
            {
                if (spellData.Linked || spellData.Rarety != rarety)
                    continue;

                if (unlocked != null)
                {
                    // if UNLOCKED is required : check that spell is already unlocked
                    if (unlocked.Value && InventoryCloudData.Instance.GetSpell(spellData.Spell).Level == 0)
                        continue;

                    // if NOT UNLOCKED is required : check that spell is not already unlocked
                    if (! unlocked.Value && InventoryCloudData.Instance.GetSpell(spellData.Spell).Level > 0)
                        continue;
                }

                spells.Add(spellData);
            }

            return spells;
        }

        public static SpellData GetRandomSpell(List<ERarety> raretyFilters = default, List<ESpellType> spellTypeFilters = default, List<EStateEffect> stateEffectFilters = default, List<ESpell> notAllowedSpellsFilter = default, bool? unlocked = null)
        {
            var spells = FilterSpells(raretyFilters, spellTypeFilters, stateEffectFilters, notAllowedSpellsFilter, unlocked);
            if (spells.Count == 0)
            {
                ErrorHandler.Warning("Unable to find any spell matching provided filters");
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, spells.Count);
            return spells[randomIndex];
        }

        /// <summary>
        /// Return a list of spells that can be filtered by :
        ///     - Rarety
        ///     - Type 
        ///     - State Effects
        /// </summary>
        /// <param name="raretyFilters"></param>
        /// <param name="spellTypeFilters"></param>
        /// <param name="stateEffectFilters"></param>
        /// <returns></returns>
        public static List<SpellData> FilterSpells(List<ERarety> raretyFilters = default, List<ESpellType> spellTypeFilters = default, List<EStateEffect> stateEffectFilters = default, List<ESpell> notAllowedSpellsFilter = default, bool? unlocked = null)
        {
            List<SpellData> spells = new List<SpellData>();
            foreach (var spellData in Instance.m_Spells.Values)
            {
                // CHECK : not linked
                if (spellData.Linked)
                    continue;

                // CHECK : not in not allowed spells
                if (notAllowedSpellsFilter != null && notAllowedSpellsFilter.Contains(spellData.Spell))
                    continue;

                // CHECK : rarety
                if (raretyFilters != null && raretyFilters.Count > 0 && !raretyFilters.Contains(spellData.Rarety))
                    continue;
                
                // CHECK : type
                if (spellTypeFilters != null && spellTypeFilters.Count > 0 && !spellTypeFilters.Contains(spellData.SpellType))
                    continue;
                
                // CHECK : spell filter
                if (stateEffectFilters != null && stateEffectFilters.Count > 0)
                {
                    var spellInfos = spellData.GetInfos();

                    // no effects on spell - continue
                    if (!spellInfos.ContainsKey("Effects"))
                        continue;

                    var spellEffects = (spellInfos["Effects"] as List<SStateEffectData>);
                    if (spellEffects.Count == 0)
                        continue;

                    var filteredEffects = spellEffects.Where(effect => stateEffectFilters.Contains(effect.StateEffect)).ToList();
                    if (filteredEffects.Count == 0)
                        continue;
                }

                // CHECK : not in not allowed spells
                if (notAllowedSpellsFilter != null && notAllowedSpellsFilter.Contains(spellData.Spell))
                    continue;

                // CHECK : is owned
                if (unlocked != null)
                {
                    // if UNLOCKED is required : check that spell is already unlocked
                    if (unlocked.Value && InventoryCloudData.Instance.GetSpell(spellData.Spell).Level == 0)
                        continue;

                    // if NOT UNLOCKED is required : check that spell is not already unlocked
                    if (!unlocked.Value && InventoryCloudData.Instance.GetSpell(spellData.Spell).Level > 0)
                        continue;
                }

                spells.Add(spellData);
            }

            return spells;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateEffectName"></param>
        /// <returns></returns>
        public static StateEffect GetStateEffect(string stateEffectName, int level = 1)
        {
            if (! Instance.m_StateEffects.ContainsKey(stateEffectName))
            {
                StateEffect state = ScriptableObject.CreateInstance<StateEffect>();
                state.name = stateEffectName;
                return state;
            }

            var clone = Instance.m_StateEffects[stateEffectName].Clone(level);
            clone.name = stateEffectName;
            return clone;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateEffect"></param>
        /// <returns></returns>
        public static StateEffect GetStateEffect(EStateEffect stateEffect, int level)
        {
            return GetStateEffect(stateEffect.ToString(), level);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rune"></param>
        /// <returns></returns>
        public static RuneData GetRuneData(ERune rune, int level = 1)
        {
            if (!Instance.m_RunesData.ContainsKey(rune))
            {
                ErrorHandler.Error("Rune not found in dict of RuneData : " + rune);
                return default;
            }

            return (RuneData)Instance.m_RunesData[rune].Clone(level);
        }

        #endregion
    }

}
