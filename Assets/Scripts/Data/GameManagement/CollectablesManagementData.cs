using Enums;
using Game.Loaders;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data.GameManagement
{
    [Serializable]
    public struct SAccountLevelData
    {
        /// <summary> quantity of gold required to level up </summary>
        public int RequiredXp;
        /// <summary> Number of cards required to level up </summary>
        public SRewardsData Rewards;

        public SAccountLevelData(int xp, SRewardsData rewards)
        {
            RequiredXp = xp;
            Rewards = rewards;
        }
    }

    [Serializable]
    public struct SLevelData
    {
        /// <summary> quantity of gold required to level up </summary>
        [FormerlySerializedAs("RequiredGolds")] 
        public int RequiredGold;
        /// <summary> Number of cards required to level up </summary>
        public int RequiredQty;

        public SLevelData(int gold, int qty)
        {
            RequiredGold = gold;
            RequiredQty = qty;
        }
    }

    [Serializable]
    public struct SMasteryUpgradeData
    {
        /// <summary> quantity of gems required to level up </summary>
        public ERarety Rarety;
        /// <summary> Number of cards required to level up </summary>
        public List<SPriceData> MasteryCosts;
    }

    [Serializable]
    public struct SMasteryPriceReductionData
    {
        /// <summary> quantity of gems required to level up </summary>
        public ECollectableType Collectable;
        /// <summary> Number of cards required to level up </summary>
        public List<SPriceData> PriceReductions;
    }

    [Serializable]
    public struct SRaretyData
    {
        public ERarety  Rarety;
        public Color    Color;
        public int      StartLevel;
    }

    [CreateAssetMenu(fileName = "CollectablesManagement", menuName = "Game/Management/Collectables")]
    public class CollectablesManagementData : ScriptableObject
    {
        #region Members

        public const int MAX_LEVEL = 14;
        public const int MAX_MASTERY = 3;

        [Tooltip("Specific data for each rarety type of spells")]
        public List<SRaretyData>                RaretyData;
        [Tooltip("Required xp of each account levelup + associated rewards")]
        public List<SAccountLevelData>          AccountLevelData;
        [Tooltip("Quantity and Gold required for each Character level up")]
        public List<SLevelData>                 CharacterLevelData;
        [Tooltip("Quantity and Gold required for each Spell level up")]
        public List<SLevelData>                 SpellLevelData;
        [Tooltip("Quantity and Gold required for each Rune level up")]
        public List<SLevelData>                 RuneLevelData;
        [Header("Mastery")]
        [Tooltip("Quantity and Gems required for each Mastery upgrade")]
        public List<SMasteryUpgradeData>        MasteryUpgradeData;
        [Tooltip("List of price reduction for each mastery upgrade")]
        public List<SMasteryPriceReductionData> MasteryPriceReductions;

        public static CollectablesManagementData s_Instance;

        public static CollectablesManagementData Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = AssetLoader.Load<CollectablesManagementData>("CollectablesManagementData", AssetLoader.c_ManagementDataPath);
                }

                return s_Instance;
            }
        }

        public static bool IsAccountUpgradable => !ProfileCloudData.IsAccountMaxed && InventoryCloudData.Instance.GetCurrency(ECurrency.TotalXp) >= GetCurrentAccountLevelData().RequiredXp;

        #endregion


        #region Check special cases

        public static bool IsBossSpell(Enum collectable)
        {
            if (!Enum.TryParse(collectable.ToString(), out ESpell spell))
                return false;

            return (int)spell >= 10000;
        }

        #endregion


        #region Data Management

        public static SAccountLevelData GetCurrentAccountLevelData()
        {
            if (ProfileCloudData.IsAccountMaxed)
                return new SAccountLevelData(0, default);

            return Instance.AccountLevelData[ProfileCloudData.AccountLevel - 1];
        }

        public static CollectableData GetData(Enum collectable, int level, bool destroy = false)
        {
            // load data of the item
            if (collectable.GetType() == typeof(ECharacter))
                return CharacterLoader.GetCharacterData((ECharacter)collectable, level, destroy: destroy);

            if (collectable.GetType() == typeof(ESpell))
                return SpellLoader.GetSpellData((ESpell)collectable, level, destroy: destroy);

            if (collectable.GetType() == typeof(ERune))
                return SpellLoader.GetRuneData((ERune)collectable, level, destroy: destroy);

            ErrorHandler.Error("Unable to find CollectionData for data " + collectable + " of type " + collectable.GetType());
            return null;
        }

        public static CollectableData GetRandomData(ECollectableType collectableType, List<Enum> notAllowed, int level, bool destroy = false)
        {
            CollectableData collectableData;

            // load data of the item
            switch(collectableType)
            {
                case ECollectableType.Character:
                    var values = Enum.GetValues(typeof(ECharacter)).Cast<ECharacter>().ToList();
                    return GetData(values.Where(value => !notAllowed.Cast<ECharacter>().ToList().Contains(value)).GetRandom(), level, destroy);
                
                case ECollectableType.Spell:
                    collectableData = SpellLoader.GetRandomSpell(notAllowedSpellsFilter: notAllowed.Cast<ESpell>().ToList());
                    break;

                case ECollectableType.Rune:
                    collectableData = SpellLoader.GetRandomRune(notAllowedFilter: notAllowed.Cast<ERune>().ToList());
                    break;

                default:
                    ErrorHandler.Error("Unhandled case " + collectableType);
                    return null;
            }

            collectableData.SetLevel(level);
            return collectableData;
        }

        public static T GetRandomCollectable<T>(List<T> notAllowed) where T : Enum
        {
            var available = Enum.GetValues(typeof(T))
                                .Cast<T>()
                                .Where(value => !notAllowed.Contains(value))
                                .ToList();

            if (available.Count == 0)
            {
                ErrorHandler.Error("No available collectables to choose from.");
                return default;
            }

            return available[new System.Random().Next(available.Count)];
        }

        #endregion


        #region Type & Cast

        public static bool IsCollectableType(Type type)
        {
            foreach (ECollectableType collectableType in Enum.GetValues(typeof(ECollectableType)))
            {
                if (collectableType == ECollectableType.None)
                    continue;

                if (GetEnumType(collectableType) == type)
                    return true;
            }

            return false;
        }

        public static Type GetEnumType(ECollectableType collectableType)
        {
            switch (collectableType)
            {
                case ECollectableType.None:
                    ErrorHandler.Warning("None collectable type was provided");
                    return null;

                case ECollectableType.Spell:
                    return typeof(ESpell);

                case ECollectableType.Character:
                    return typeof(ECharacter);

                case ECollectableType.Rune:
                    return typeof(ERune);

                default:
                    ErrorHandler.Warning("Unhandled type provided : " + collectableType.ToString());
                    return null;
            }
        }

        public static bool TryGetCollectableType(Enum collectable, out ECollectableType collectableType, bool logError = false)
        {
            if (! Enum.TryParse(collectable.GetType().ToString().Split(".")[1][1..], out collectableType))
            {
                if (logError)
                    ErrorHandler.Error("unable to parse " + collectable.GetType().ToString() + " into ECollectableType");
                return false;
            }

            return true;
        }

        public static bool TryCast(string name, Type collectableType, out Enum collectable)
        {
            collectable = null;
            if (Enum.TryParse(collectableType, name, out object result))
            {
                collectable = (Enum)result;
                return true;
            }

            return false;
        }

        public static Enum Cast(string name, ECollectableType collectableType)
        {
            return Cast(name, GetEnumType(collectableType));
        }

        public static Enum Cast(string name, Type collectableType)
        {
            if (Enum.TryParse(collectableType, name, out object result))
                return (Enum)result;

            ErrorHandler.Error("Failed to parse enum value for value: " + name);
            return null;
        }

        #endregion


        #region Account Level

        public static bool IsAccountUpgradableIn(int bonusXp)
        {
            return !ProfileCloudData.IsAccountMaxed
                && InventoryCloudData.Instance.GetCurrency(ECurrency.TotalXp) + bonusXp >= GetCurrentAccountLevelData().RequiredXp;
        }


        #endregion


        #region Level & Rarety 

        public static int GetStartLevel(Enum collectable)
        {
            // start level of characters is always 1, indepedently of the rarity
            if (collectable.GetType() == typeof(ECharacter))
                return 1;
            
            return GetRaretyData(collectable).StartLevel;
        }

        public static int GetMaxLevel(Enum collectable)
        {
            // start level of characters is always 1, indepedently of the rarity
            if (collectable.GetType() == typeof(ECharacter))
                return Instance.CharacterLevelData.Count + 1;

            if (collectable.GetType() == typeof(ESpell) || collectable.GetType() == typeof(ERune))
                return Instance.SpellLevelData.Count + 1;

            ErrorHandler.Error("Unhandled type of level data for collectable " + collectable);
            return 1;
        }

        /// <summary>
        /// Get the data for a specific rarety
        /// </summary>
        /// <param name="rarety"></param>
        /// <returns></returns>
        public static SRaretyData GetRaretyData(ERarety rarety)
        {
            foreach (var raretyData in Instance.RaretyData)
            {
                if (raretyData.Rarety == rarety)
                    return raretyData;
            }

            ErrorHandler.Error("Unable to find rarety data for " + rarety);
            return Instance.RaretyData[0];
        }

        /// <summary>
        /// Get the data for a specific collectable (character, spell, rune, ...)
        /// </summary>
        /// <param name="collectable"> value of a collectable </param>
        /// <returns></returns>
        public static SRaretyData GetRaretyData(Enum collectable)
        {
            return GetRaretyData(GetData(collectable, 1, destroy: true).Rarety);
        }

        /// <summary>
        /// Get the Level Up data of the provided value (required gold, quantity, ...)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="rarety"></param>
        /// <returns></returns>
        public static SLevelData GetLevelData(Enum collectable, int level, int mastery = 0)
        {
            // error control
            if (level < 0 || level > GetMaxLevel(collectable))
                ErrorHandler.Warning("bad level provided : " + level);

            // no level yet - unlocked
            if (level <= 0)
                return new SLevelData(0, 1);

            // max level - return (0, 0)
            if (level >= GetMaxLevel(collectable))
                return new SLevelData(0, 0);

            SLevelData levelData;
            // character has its own level up values (gold, qty, ...) and is not dependent on rarety
            if (collectable.GetType() == typeof(ECharacter))
                levelData = GetCharacterLevelData(level);

            // Rune & Spells have same level up data
            else if (collectable.GetType() == typeof(ESpell))
                levelData = GetSpellLevelData(level, GetRaretyData(collectable).Rarety);

            // Rune & Spells have same level up data
            else if (collectable.GetType() == typeof(ERune))
                levelData = GetRuneLevelData(level, GetRaretyData(collectable).Rarety);

            else
            {
                ErrorHandler.Error("unable to find level data for " + collectable);
                return default;
            }

            // --------------------------------------------------------------------------
            // CHECK if there are reductions set for this Enum at this mastery
            // NOTE : ERROR DISEABLED caus atm, this is not fully set yet.
            //        PUT "withError: true" as soon as possible
            if (TryGetMasteryPriceReduction(collectable, mastery, out SPriceData priceReduction, withError: false))
            {
                if (priceReduction.Currency == ECurrency.Xp)
                    levelData.RequiredQty = (int)Math.Round((1 - priceReduction.Price) * levelData.RequiredQty);

                else if (priceReduction.Currency == ECurrency.Gold)
                    levelData.RequiredGold = (int)Math.Round((1 - priceReduction.Price) * levelData.RequiredGold);

                else
                    ErrorHandler.Warning("Unhandled type of reduction : " + priceReduction.Currency);
            }

            return levelData;
        }

        /// <summary>
        /// Get the Character's Level Up data  
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static SLevelData GetCharacterLevelData(int level)
        {
            if (level <= 0)
            {
                ErrorHandler.Error("Provided level is <= 0 : " + level);
                level = 1;
            }

            if (level > Instance.CharacterLevelData.Count)
            {
                ErrorHandler.Error("CharacterLevelData requested for level " + level + " while max level is " + Instance.CharacterLevelData.Count + 1);
                return new SLevelData(0, 0);
            }

            var data = Instance.CharacterLevelData[level - 1];
            return data;
        }

        /// <summary>
        /// Get the Spell Level Up data depending on the rarety and the level of the spell
        /// </summary>
        /// <param name="level"></param>
        /// <param name="rarety"></param>
        /// <returns></returns>
        public static SLevelData GetSpellLevelData(int level, ERarety? rarety = null)
        {
            int levelIndex = level - 1;
            if (rarety.HasValue)
            {
                SRaretyData raretyData = GetRaretyData(rarety.Value);
                levelIndex = level - raretyData.StartLevel;
            }

            if (levelIndex < 0 || levelIndex >= Instance.SpellLevelData.Count)
            {
                ErrorHandler.Error($"bad level index ({levelIndex}) for rarety ({rarety})");
                levelIndex = 0;
            }

            return new SLevelData(Instance.SpellLevelData[level - 1].RequiredGold, Instance.SpellLevelData[levelIndex].RequiredQty);
        }

        /// <summary>
        /// Get the Spell Level Up data depending on the rarety and the level of the spell
        /// </summary>
        /// <param name="level"></param>
        /// <param name="rarety"></param>
        /// <returns></returns>
        public static SLevelData GetRuneLevelData(int level, ERarety? rarety = null)
        {
            int levelIndex = level - 1;
            if (rarety.HasValue)
            {
                SRaretyData raretyData = GetRaretyData(rarety.Value);
                levelIndex = level - raretyData.StartLevel;
            }

            if (levelIndex < 0 || levelIndex >= Instance.SpellLevelData.Count)
            {
                ErrorHandler.Error($"bad level index ({levelIndex}) for rarety ({rarety})");
                levelIndex = 0;
            }

            return new SLevelData(Instance.RuneLevelData[level - 1].RequiredGold, Instance.RuneLevelData[levelIndex].RequiredQty);
        }

        #endregion


        #region Mastery

        /// <summary>
        /// Get all mastery upgrade data for a certain rarety
        /// </summary>
        /// <param name="rarety"></param>
        /// <param name="masteryUpradeData"></param>
        /// <returns></returns>
        public static bool TryGetMasteryUpgradeData(ERarety rarety, out SMasteryUpgradeData masteryUpradeData) 
        {
            masteryUpradeData = new();
            var temp = Instance.MasteryUpgradeData.Where(data => data.Rarety == rarety).ToList();
            if (temp.Count != 1)
            {
                ErrorHandler.Error("Bad data config : found " + temp.Count + " MasteryUpgradeData with rarety = " + rarety);
                return false;
            }

            masteryUpradeData = temp[0];
            return true;
        }

        /// <summary>
        /// Get the cost/currency value to upgrade a specific rarety at a specific mastery
        /// </summary>
        /// <param name="rarety"></param>
        /// <param name="currentMastery"></param>
        /// <param name="currencyCost"></param>
        /// <returns></returns>
        public static bool TryGetMasteryUpgradeCost(ERarety rarety, int currentMastery, out SPriceData currencyCost)
        {
            currencyCost = new();
            if (! TryGetMasteryUpgradeData(rarety, out SMasteryUpgradeData masteryUpradeData))
                return false;

            if (currentMastery >= masteryUpradeData.MasteryCosts.Count)
            {
                ErrorHandler.Error("Bad data config : currentMastery (" + currentMastery + ") >= number of masteries upgrade " + masteryUpradeData.MasteryCosts.Count);
                return false;
            }

            currencyCost = masteryUpradeData.MasteryCosts[currentMastery];
            return true;
        }

        public static bool TryGetMasteryPriceReduction(Enum collectable, int mastery, out SPriceData priceReduction, bool withError = true)
        {
            priceReduction = new();
            if (!TryGetCollectableType(collectable, out ECollectableType collectableType, true))
                return false;

            return TryGetMasteryPriceReduction(collectableType, mastery, out priceReduction, withError);
        }
            
        public static bool TryGetMasteryPriceReduction(ECollectableType collectableType, int mastery, out SPriceData priceReduction, bool withError = true)
        {
            priceReduction = new();

            if (mastery < 0)
            {
                ErrorHandler.Error("Bad mastery provided : " + mastery);
                return false;
            }

            // if mastery = 0 just return no price reduction
            if (mastery == 0)
            {
                return true;
            }

            var values = Instance.MasteryPriceReductions.Where(t => t.Collectable == collectableType).ToList();
            if (values.Count == 0)
            {
                if (withError)
                    ErrorHandler.Warning("Unable to find any price reduction data for " + collectableType);
                return false;
            }

            if (values.Count > 1)
            {
                if (withError) 
                    ErrorHandler.Warning("Found multiple MasteryPriceReduction for collectable type : " + collectableType);
            }

            if (values[0].PriceReductions.IsNullOrEmpty())
            {
                if (withError)
                    ErrorHandler.Warning("No price reduction is set for coolectable type : " + collectableType);
            }

            if (values[0].PriceReductions.Count < mastery)
            {
                if (withError) 
                    ErrorHandler.Warning("Mastery ("+ mastery + ") is above PriceRecutions max index (" + values[0].PriceReductions.Count + ") for collectable type " + collectableType);

                priceReduction = values[0].PriceReductions[^1];
            } 
            else
            {
                priceReduction = values[0].PriceReductions[mastery - 1];
            }

            // CHECK : Price between 0 and 1
            if (priceReduction.Price < 0)
            {
                ErrorHandler.Warning("Bad reduction set : reduction must be >= 0");
                priceReduction.Price = Mathf.Abs(priceReduction.Price);
            }
            if (priceReduction.Price > 1)
            {
                ErrorHandler.Warning("Bad reduction set : reduction must be <= 1");
                priceReduction.Price = 1f;
            }

            return true;
        }

        #endregion


        #region Conversion

        public static int ConvertCharacterToXp(ECharacter character)
        {
            switch (GetRaretyData(character).Rarety)
            {
                case ERarety.Common:
                    return 100;

                case ERarety.Rare:
                    return 300;

                case ERarety.Epic:
                    return 3000;

                case ERarety.Legendary:
                    return 10000;

                default:
                    ErrorHandler.Warning("Unhanlded case : " + GetRaretyData(character).Rarety);
                    return 0;
            }
        }

        #endregion


        #region Order & Filters

        /// <summary>
        /// Order collectables by a specific metric
        /// </summary>
        /// <typeparam name="T">Type of collectable (or a derived class of CollectableData)</typeparam>
        /// <param name="collectables"></param>
        /// <param name="orderBy"></param>
        /// <returns>Ordered list of collectables</returns>
        public static List<T> OrderCollectable<T>(List<T> collectables, EOrderBy orderBy = EOrderBy.Rarety) where T : CollectableData
        {
            switch (orderBy)
            {
                case EOrderBy.Rarety:
                    // Sort by rarety first, then by level in case of ties
                    return collectables.OrderBy(collectable => collectable.Rarety)
                                   .ThenBy(collectable => collectable.Level)
                                   .ToList();

                case EOrderBy.Level:
                    // Sort by level first, then by rarety in case of ties
                    return collectables.OrderByDescending(collectable => collectable.Level)
                                   .ThenBy(collectable => collectable.Rarety)
                                   .ToList();

                case EOrderBy.None:
                default:
                    // No sorting if EOrderBy.None is selected
                    return collectables;
            }
        }

        #endregion

    }
}