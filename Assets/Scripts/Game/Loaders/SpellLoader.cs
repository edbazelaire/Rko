using Assets.Scripts.Data.PowerUp;
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
    public static class SpellLoader
    {
        #region Members

        public static bool Initialized { get; private set; }

        static Dictionary<string, GameObject>      m_SpellsPrefabs;
        static Dictionary<ESpell, SpellData>       m_Spells;
        static Dictionary<string, SpellData>       m_OnHitSpellData;
        static Dictionary<string, StateEffect>     m_StateEffects;
        static Dictionary<string, RuneData>        m_RunesData;

        public static List<ESpell> Spells                       => m_Spells.Keys.ToList();
        public static List<SpellData> SpellsData                => m_Spells.Values.ToList();
        public static Dictionary<string, RuneData> RunesData    => m_RunesData;

        #endregion


        #region Initialization & Loading

        public static void Initialize()
        {
            InitializeSpellPrefabs();
            InitializeSpells();
            InitializeStateEffects();
            InitializeRuneData();

            Initialized = true;
        }

        static void InitializeSpellPrefabs()
        {
            m_SpellsPrefabs = new Dictionary<string, GameObject>();

            GameObject[] spellPrefabs = AssetLoader.LoadSpellPrefabs();
            foreach (GameObject spellPrefab in spellPrefabs)
            {
                m_SpellsPrefabs.Add(spellPrefab.name, spellPrefab);
            }
        }

        static void InitializeSpells()
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
                    ErrorHandler.Error($"SpellLoader : AnimationTimer {spell.Spell} < 0");

                if (spell.Spell == ESpell.None)
                {
                    ErrorHandler.Error("Unable to parse " + spell.Name + " as Spell");
                    continue;
                }

                m_Spells.Add(spell.Spell, spell);
            }
        }

        static void InitializeStateEffects()
        {
            StateEffect[] allData = LoadStateEffects();

            m_StateEffects = new Dictionary<string, StateEffect>();

            foreach (StateEffect data in allData)
            {
                m_StateEffects.Add(data.name, data);
            }
        }

        static void InitializeRuneData()
        {
            RuneData[] allData = LoadRunesData();

            m_RunesData = new Dictionary<string, RuneData>();
            foreach (RuneData data in allData)
            {
                m_RunesData.Add(data.Name, data);
            }
        }

        static SpellData[] LoadSpells()
        {
           return Resources.LoadAll<SpellData>("Data/Spells");
        }

        static StateEffect[] LoadStateEffects()
        {
           return Resources.LoadAll<StateEffect>("Data/StateEffects");
        }

        static RuneData[] LoadRunesData()
        {
            var data = AssetLoader.LoadAll<RuneData>(AssetLoader.c_PowerUpsPath).ToList();
            data.AddRange(Resources.LoadAll<RuneData>("Data/Runes"));
            return data.ToArray();
            
        }

        #endregion


        #region Data Management Accessors

        public static SRaretyData GetRaretyData(ERarety rarety)
        {
            return CollectablesManagementData.GetRaretyData(rarety);
        }

        public static SRaretyData GetRaretyData(ESpell spell)
        {
            return CollectablesManagementData.GetRaretyData(m_Spells[spell].Rarety);
        }

        public static SLevelData GetSpellLevelData(ESpell spell)
        {
            SCollectableCloudData data = InventoryManager.GetSpellData(spell);
            if (data.Level == 0)
                InventoryManager.Unlock(ref data);
            return CollectablesManagementData.GetSpellLevelData(data.Level, m_Spells[spell].Rarety);
        }

        public static List<RuneData> GetPlayerRunesData()
        {
            var runesData = new List<RuneData>();
            foreach (var item in m_RunesData)
            {
                if (Enum.TryParse(item.Key, out ERune _))
                {
                    runesData.Add(item.Value);
                }
            }

            return runesData;
        }

        #endregion


        #region Static Manipulators

        /// <summary>
        /// Check if spell exists
        /// </summary>
        /// <param name="spellName"></param>
        /// <returns></returns>
        public static bool SpellExists(string name)
        {
            return Enum.TryParse(name, out ESpell _) || m_OnHitSpellData.ContainsKey(name);
        }

        /// <summary>
        /// Check if spell exists
        /// </summary>
        /// <param name="spellName"></param>
        /// <returns></returns>
        public static bool StateEffectExists(string name)
        {
            return Enum.TryParse(name, out EStateEffect _) || m_StateEffects.ContainsKey(name);
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
            if (m_SpellsPrefabs.ContainsKey(spellName))
                return m_SpellsPrefabs[spellName];

            // check that exists
            if (! m_SpellsPrefabs.ContainsKey(spellType.ToString()))
                ErrorHandler.FatalError("Unable to find default prefab for spell type " + spellType.ToString());

            // returns default prefab for spell type
            return m_SpellsPrefabs[spellType.ToString()];
        } 

        /// <summary>
        /// Get the spell data of the given spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public static SpellData GetSpellData(ESpell spell, int level = 1, bool destroy = false)
        {
            if (!m_Spells.ContainsKey(spell))
            {
                ErrorHandler.Error($"SpellLoader : Spell {spell} not found");
                return null;
            }

            var spellData = m_Spells[spell].Clone(level, destroy);

            return spellData;
        }

        /// <summary>
        /// Get the spell data of the given spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public static SpellData GetSpellData(string spellName, int level = 1, bool destroy = false)
        {
            if (Enum.TryParse(spellName, out ESpell spell))
            {
                return GetSpellData(spell, level, destroy);
            }

            if (m_OnHitSpellData.ContainsKey(spellName))
            {
                return m_OnHitSpellData[spellName].Clone(level, destroy);
            }
            
            ErrorHandler.Error($"SpellLoader : Spell {spellName} not found");
            return null;
        }

        /// <summary>
        /// Get only spell's description and destroy the Instance after 
        /// </summary>
        /// <param name="spellName"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string GetSpellDescription(string spellName, int level = 1)
        {
            var spellData = GetSpellData(spellName, level);
            string description = spellData.GetDescription();
            GameObject.Destroy(spellData);
            return description;
        }

        /// <summary>
        /// Get only spell's info dict and destroy the Instance after 
        /// </summary>
        /// <param name="spellName"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetSpellInfos(string spellName, int level = 1)
        {
            var spellData = GetSpellData(spellName, level);
            var infos = spellData.GetInfos();
            GameObject.Destroy(spellData);
            return infos;
        }

        public static bool IsInstantanious(string stateEffectName)
        {
            var data = GetStateEffect(stateEffectName);
            bool isInst = data.IsInstantanious;
            GameObject.Destroy(data);
            return isInst;
        }

        public static bool IsLinked(string spellName)
        {
            var spellData = GetSpellData(spellName);
            bool isLinked = spellData.Linked;
            GameObject.Destroy(spellData);
            return isLinked;
        }

        /// <summary>
        /// Get all spells of a specific rarety
        /// </summary>
        /// <param name="rarety"></param>
        /// <returns></returns>
        public static List<SpellData> GetSpellsFromRarety(ERarety rarety, bool? unlocked = null) 
        { 
            List<SpellData> spells = new List<SpellData>();
            foreach (var spellData in m_Spells.Values)
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

        /// <summary>
        /// Get a spell matching provided filters
        /// </summary>
        /// <param name="raretyFilters"></param>
        /// <param name="spellTypeFilters"></param>
        /// <param name="spellElementFilters"></param>
        /// <param name="stateEffectFilters"></param>
        /// <param name="notAllowedSpellsFilter"></param>
        /// <param name="unlocked"></param>
        /// <returns></returns>
        public static SpellData GetRandomSpell(List<ERarety> raretyFilters = default, List<ESpellType> spellTypeFilters = default, List<ESpellElement> spellElementFilters = default, List<EStateEffect> stateEffectFilters = default, List<ESpell> notAllowedSpellsFilter = default, bool? unlocked = null)
        {
            var spells = FilterSpells(raretyFilters, spellTypeFilters, spellElementFilters, stateEffectFilters, notAllowedSpellsFilter, unlocked);
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
        public static List<SpellData> FilterSpells(List<ERarety> raretyFilters = default, List<ESpellType> spellTypeFilters = default, List<ESpellElement> spellElementFilters = default, List<EStateEffect> stateEffectFilters = default, List<ESpell> notAllowedSpellsFilter = default, bool? unlocked = null, string containsName = "")
        {
            List<SpellData> spells = new List<SpellData>();
            foreach (var spellData in m_Spells.Values)
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

                // CHECK : Spell Element
                if (spellElementFilters != null && spellElementFilters.Count > 0)
                {
                    if (spellData.SpellElements == null || spellData.SpellElements.Count == 0)
                    {
                        // CHECK : NEUTRAL type
                        if (! spellElementFilters.Contains(ESpellElement.Neutral))
                            continue;
                    }

                    // CHECK : has at least one of required elements
                    else if (spellData.SpellElements.Where(element => spellElementFilters.Contains(element)).ToList().Count() == 0)
                        continue;
                }
                
                // FILTER : State Effects
                if (stateEffectFilters != null && stateEffectFilters.Count > 0)
                {
                    var spellInfos = spellData.GetInfos();

                    // no effects on spell - continue
                    if (!spellInfos.ContainsKey("Effects"))
                        continue;

                    var spellEffects = (spellInfos["Effects"] as List<SStateEffectData>);
                    if (spellEffects.Count == 0)
                        continue;

                    // CHECK : at least one of the effects of the spell is one of the requested effects
                    if (spellEffects.Where(effect => stateEffectFilters.Contains(effect.StateEffect)).ToList().Count == 0)
                        continue;
                }

                // FILTER : not in not allowed spells
                if (notAllowedSpellsFilter != null && notAllowedSpellsFilter.Contains(spellData.Spell))
                    continue;

                // FILTER : is owned
                if (unlocked != null)
                {
                    // if UNLOCKED is required : check that spell is already unlocked
                    if (unlocked.Value && InventoryCloudData.Instance.GetSpell(spellData.Spell).Level == 0)
                        continue;

                    // if NOT UNLOCKED is required : check that spell is not already unlocked
                    if (!unlocked.Value && InventoryCloudData.Instance.GetSpell(spellData.Spell).Level > 0)
                        continue;
                }

                // FILTER : name contains string
                if (! string.IsNullOrEmpty(containsName) && ! spellData.Spell.ToString().ToLower().Contains(containsName.ToLower()))
                   continue;

                spells.Add(spellData);
            }

            return spells;
        }

        /// <summary>
        /// Order spells by a specific metric
        /// </summary>
        /// <param name="spells"></param>
        /// <param name="orderBy"></param>
        public static List<SpellData> OrderSpells(List<SpellData> spells, EOrderBy orderBy = EOrderBy.Rarety)
        {
            switch (orderBy)
            {
                case EOrderBy.Rarety:
                    // Sort by rarety first, then by level in case of ties
                    return spells.OrderBy(spell => spell.Rarety)
                                   .ThenBy(spell => spell.Level)
                                   .ToList();

                case EOrderBy.Level:
                    // Sort by level first, then by rarety in case of ties
                    return spells.OrderByDescending(spell => spell.Level)
                                   .ThenBy(spell => spell.Rarety)
                                   .ToList();

                case EOrderBy.None:
                default:
                    // No sorting if EOrderBy.None is selected
                    return spells;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateEffectName"></param>
        /// <returns></returns>
        public static StateEffect GetStateEffect(string stateEffectName, int level = 1)
        {
            StateEffect stateEffect;
            if (! m_StateEffects.ContainsKey(stateEffectName))
            {
                stateEffect = ScriptableObject.CreateInstance<StateEffect>(); 
            } 
            else
            {
                stateEffect = m_StateEffects[stateEffectName].Clone(level);
            }

            stateEffect.name = stateEffectName;
            return stateEffect;
        }

        /// <summary>
        /// Get the description of a state effect and destroy the instance right after
        /// </summary>
        /// <param name="stateEffectName"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string GetStateEffectDescription(string stateEffectName, int level = 1)
        {
            StateEffect stateEffect = GetStateEffect(stateEffectName, level);
            string description = stateEffect.GetDescription();
            GameObject.Destroy(stateEffect);
            return description;
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

        public static RuneData GetRuneData(ERune rune, int level = 1, bool destroy = false)
        {
            return GetRuneData(rune.ToString(), level, destroy);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rune"></param>
        /// <returns></returns>
        public static RuneData GetRuneData(string rune, int level = 1, bool destroy = false)
        {
            if (!m_RunesData.ContainsKey(rune))
            {
                ErrorHandler.Error("Rune not found in dict of RuneData : " + rune);
                return default;
            }

            var data = (RuneData)m_RunesData[rune].Clone(level);
            if (destroy)
                CoroutineManager.DelayMethod(() => GameObject.Destroy(data));

            return data;
        }

        /// <summary>
        /// Get a spell matching provided filters
        /// </summary>
        /// <param name="raretyFilters"></param>
        /// <param name="spellTypeFilters"></param>
        /// <param name="spellElementFilters"></param>
        /// <param name="stateEffectFilters"></param>
        /// <param name="notAllowedSpellsFilter"></param>
        /// <param name="unlocked"></param>
        /// <returns></returns>
        public static RuneData GetRandomRune(List<ERarety> raretyFilter = default, List<ESpellElement> elementsFilter = default, List<ERune> notAllowedFilter = default, bool? unlocked = null, string containsName = "")
        {
            var runes = FilterRunes(raretyFilter, elementsFilter, notAllowedFilter, unlocked, containsName);
            if (runes.Count == 0)
                return null;

            int randomIndex = UnityEngine.Random.Range(0, runes.Count);
            return runes[randomIndex];
        }

        /// <summary>
        /// Return a list of spells that can be filtered by :
        ///     - Rarety
        ///     - Type 
        ///     - State Effects
        /// </summary>
        /// <param name="raretyFilter">        allowed types of rarety for the runes                           </param>
        /// <param name="elementsFilter">  allowed elements of the runes                                   </param>
        /// <param name="notAllowedFilter">     list of not runes that are not allowed to be in the return data </param>
        /// <returns></returns>
        public static List<RuneData> FilterRunes(List<ERarety> raretyFilter = default, List<ESpellElement> elementsFilter = default, List<ERune> notAllowedFilter = default, bool? unlocked = null, string containsName = "")
        {
            List<RuneData> runes = new List<RuneData>();
            foreach (var runeData in m_RunesData.Values)
            {
                // CHECK : is None
                if (runeData.Rune == ERune.None)
                    continue;

                // CHECK : not in not allowed spells
                if (notAllowedFilter != null && notAllowedFilter.Contains(runeData.Rune))
                    continue;

                // CHECK : rarety
                if (raretyFilter != null && raretyFilter.Count > 0 && !raretyFilter.Contains(runeData.Rarety))
                    continue;

                // CHECK : Spell Element
                if (elementsFilter != null && elementsFilter.Count > 0)
                {
                    if (runeData.SpellElements == null || runeData.SpellElements.Count == 0)
                    {
                        // CHECK : NEUTRAL type
                        if (!elementsFilter.Contains(ESpellElement.Neutral))
                            continue;
                    }

                    // CHECK : has at least one of required elements
                    else if (runeData.SpellElements.Where(element => elementsFilter.Contains(element)).ToList().Count() == 0)
                        continue;
                }

                // FILTER : not in not allowed spells
                if (notAllowedFilter != null && notAllowedFilter.Contains(runeData.Rune))
                    continue;

                // FILTER : is owned
                if (unlocked != null)
                {
                    // if UNLOCKED is required : check that spell is already unlocked
                    if (unlocked.Value && InventoryCloudData.Instance.GetCollectable(runeData.Rune).Level == 0)
                        continue;

                    // if NOT UNLOCKED is required : check that spell is not already unlocked
                    if (!unlocked.Value && InventoryCloudData.Instance.GetCollectable(runeData.Rune).Level > 0)
                        continue;
                }

                // FILTER : name contains string
                if (!string.IsNullOrEmpty(containsName) && !runeData.Rune.ToString().ToLower().Contains(containsName.ToLower()))
                    continue;

                runes.Add(runeData);
            }

            return runes;
        }

        public static SRunePower GetPowerUp(string powerUpName, int level = 1)
        {
            if (! SRunePower.TrySplitPowerUpName(powerUpName, out string runeName, out ERuneActivation runeActivation, throwError: true))
                return null;

            return GetRuneData(runeName, level).GetRunePower(runeActivation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="raretyFilter"></param>
        /// <param name="elementsFilter"></param>
        /// <param name="notAllowedFilter"></param>
        /// <param name="unlocked"></param>
        /// <param name="containsName"></param>
        /// <returns></returns>
        public static SRunePower GetRandomPowerUp(List<ERuneActivation> runeActivationFilter = default, List<string> notAllowedFilter = default,string containsName = "")
        {
            var powerUps = FilterPowerUps(runeActivationFilter, notAllowedFilter, containsName);
            if (powerUps.Count == 0)
                return null;

            int randomIndex = UnityEngine.Random.Range(0, powerUps.Count);
            return powerUps[randomIndex];
        }

        /// <summary>
        /// Return a list of spells that can be filtered by :
        ///     - Rarety
        ///     - Type 
        ///     - State Effects
        /// </summary>
        /// <param name="raretyFilter">        allowed types of rarety for the runes                       </param>
        /// <param name="elementsFilter">  allowed elements of the runes                                   </param>
        /// <param name="notAllowedFilter">     list of not runes that are not allowed to be in the return data </param>
        /// <returns></returns>
        public static List<SRunePower> FilterPowerUps(List<ERuneActivation> runeActivationFilter = default, List<string> notAllowedFilter = default, string containsName = "")
        {
            List<SRunePower> filteredData = new List<SRunePower>();

            for (int i = 0; i < m_RunesData.Count; i++)
            {
                // clone the data to avoid overwritting
                RuneData runeData = m_RunesData.Values.ToList()[i].Clone();

                if (runeData.Name == "None")
                    continue;

                // get throught each activation level to collect as SRunePower
                foreach (ERuneActivation runeActivation in Enum.GetValues(typeof(ERuneActivation)))
                {
                    if (runeActivation == ERuneActivation.None)
                        continue;

                    SRunePower data = runeData.GetRunePower(runeActivation);

                    // CHECK : activation
                    if (runeActivationFilter != null && runeActivationFilter.Count > 0 && ! runeActivationFilter.Contains(data.RuneActivation))
                        continue;

                    // FILTER : not in not allowed spells
                    if (notAllowedFilter != null && notAllowedFilter.Contains(data.Name))
                        continue;

                    // FILTER : name contains string
                    if (!string.IsNullOrEmpty(containsName) && !data.Name.ToLower().Contains(containsName.ToLower()))
                        continue;

                    filteredData.Add(data);
                }
            }

            return filteredData;
        }


        #endregion
    }

}
