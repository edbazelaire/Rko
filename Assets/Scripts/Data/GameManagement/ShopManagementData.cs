using Enums;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.ComponentModel;
using Tools;
using Inventory;
using Save;

namespace Data.GameManagement
{
    [Serializable]
    public struct SPriceData
    {
        public int Price;
        public ECurrency Currency;

        public SPriceData(int price, ECurrency currency)
        {
            Price = price;
            Currency = currency;
        }
    }

    [Serializable]
    public struct SCollectableReward
    {
        public ECollectableType CollectableType;
        public string           CollectableName;
        public int              Qty;

        public SCollectableReward(ECollectableType collectableType, string collectableName, int qty)
        {
            this.CollectableType = collectableType;
            this.CollectableName = collectableName;
            this.Qty = qty;
        }
    }

    [Serializable]
    public struct SCurrencyReward
    {
        public ECurrency    Currency;
        public int          Qty;

        public SCurrencyReward(ECurrency currency, int qty)
        {
            this.Currency = currency;
            this.Qty = qty;
        }
    }

    [Serializable]
    public struct SRewardsData
    {
        public List<SCurrencyReward>        Currencies;
        public List<EChest>                 Chests;
        public List<SCollectableReward>     Collectables;
        public List<SAchievementReward>     AchievementRewards;

        public readonly bool IsEmpty => Count == 0;
        public readonly int Count => Currencies.Count + Chests.Count + Collectables.Count + AchievementRewards.Count;

        public SRewardsData(List<SCurrencyReward> currencyRewards = null, List<EChest> chests = null, List<SCollectableReward> collectableRewards = null, List<SAchievementReward> achievementRewards = null)
        {
            Currencies          = currencyRewards ?? new List<SCurrencyReward>();
            Chests              = chests ?? new List<EChest>();
            Collectables        = collectableRewards ?? new List<SCollectableReward>();
            AchievementRewards  = achievementRewards ?? new List<SAchievementReward>();
        }

        public void SetDefaultData()
        {
            Currencies          ??= new List<SCurrencyReward>();
            Chests              ??= new List<EChest>();
            Collectables        ??= new List<SCollectableReward>();
            AchievementRewards  ??= new List<SAchievementReward>();

        }

        public void Add(Enum item, int qty)
        {
            if (item.GetType() == typeof(ECurrency))
            {
                Currencies ??= new List<SCurrencyReward>();
                Currencies.Add(new SCurrencyReward((ECurrency)item, qty));
            } 
            
            else if (item.GetType() == typeof(EChest))
            {
                Chests ??= new List<EChest>();
                Chests.Add((EChest)item);
            } 
            
            else if ( CollectablesManagementData.TryGetCollectableType(item, out ECollectableType collectableType))
            {
                Collectables ??= new List<SCollectableReward>();
                Collectables.Add(new SCollectableReward(collectableType, item.ToString(), qty));
            } 
            
            else if (ProfileCloudData.TryGetType(item, out EAchievementReward arType, throwError: false))
            {
                AchievementRewards ??= new List<SAchievementReward>();
                var ar = new SAchievementReward();
                ar.Set(item);
                AchievementRewards.Add(ar);
            } 

            else
                ErrorHandler.Error("Unhandled type of item " + item.GetType());

        }

        public List<SReward> Rewards
        {
            get
            {
                List<SReward> list = AsRewardStruct(Currencies);
                list.AddRange(AsRewardStruct(Chests));
                list.AddRange(AsRewardStruct(Collectables));
                list.AddRange(AsRewardStruct(AchievementRewards));

                return list;
            }
        }

        public List<SReward> AsRewardStruct(List<SCurrencyReward> currencies)
        {
            if (currencies == null || currencies.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (SCurrencyReward data in currencies)
            {
                rewards.Add(new SReward(typeof(ECurrency), data.Currency.ToString(), data.Qty));
            }

            return rewards;
        }

        public List<SReward> AsRewardStruct(List<EChest> chests)
        {
            if (chests == null || chests.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (EChest data in chests)
            {
                rewards.Add(new SReward(typeof(EChest), data.ToString(), 1));
            }

            return rewards;
        }

        public List<SReward> AsRewardStruct(List<SCollectableReward> collectables)
        {
            if (collectables == null || collectables.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (SCollectableReward data in collectables)
            {
                rewards.Add(new SReward(CollectablesManagementData.GetEnumType(data.CollectableType), data.CollectableName, data.Qty));
            }

            return rewards;
        }

        public List<SReward> AsRewardStruct(List<SAchievementReward> achievementRewards)
        {
            if (achievementRewards == null || achievementRewards.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (SAchievementReward data in achievementRewards)
            {
                rewards.Add(new SReward(ProfileCloudData.GetTypeOf(data.AchievementReward), data.Value, 1));
            }

            return rewards;
        }
    }

    [Serializable]
    public struct SRaretyPriceData
    {
        public ERarety      Rarety;
        public SPriceData       Price;    

        public SRaretyPriceData(ERarety rarety, SPriceData price)
        {
            Rarety      = rarety;
            Price       = price;
        }
    }

    [Serializable]
    public struct SShopData
    {
        public string Name;
        /// <summary> list of chests in this bundle </summary>
        public Sprite Icon;
        /// <summary> list of chests in this bundle </summary>
        public SRewardsData Rewards;
        /// <summary> currency used to pay for this bundle </summary>
        public ECurrency Currency;
        /// <summary> amount of necessary currency required </summary>
        public float Cost;
        /// <summary> max number of time this can be collectd (set to 0 for no restriction) </summary>
        public int MaxCollection;
        /// <summary> percentage of reduction to apply on the price (between 0 & 1) </summary>
        public float Reduction;
        public float Price => (1 - Reduction) * Cost;

        public SShopData(string name, Sprite icon, SRewardsData rewards, ECurrency currency, int cost, int maxCollection, float reduction = 0)
        {
            Name            = name;
            Icon            = icon;
            Rewards         = rewards;
            Currency        = currency;
            Cost            = cost;
            MaxCollection   = maxCollection;

            if (reduction < 0 || reduction > 1)
            {
                ErrorHandler.Error("Trying to apply reduction ("+ reduction + ") not between 0 and 1 on " + Name);
                Reduction = 0;
            } else
            {
                Reduction = reduction;
            }
        }

        public void ApplyReduction(float reduction) 
        { 
            if (reduction < 0 || reduction > 1) 
            {
                ErrorHandler.Error("Trying to apply reduction (" + reduction + ") not between 0 and 1 on " + Name);
                return;
            }

            Reduction = reduction;
        }
    }

    [Serializable]
    public struct SCurrencyColor
    {
        public ECurrency Currency;
        public Color Color;
    }

    [CreateAssetMenu(fileName = "ShopManagementData", menuName = "Game/Management/Shop")]
    public class ShopManagementData : ScriptableObject
    {
        #region Members

        // ===============================================================================
        // CONFIG
        [Header("Configuration")]
        [Description("Price per hours to unlock a chest")]
        [SerializeField] private float m_FastUnlockChestPrice;
        [Description("Color code of currencies")]
        [SerializeField] private List<SCurrencyColor> m_CurrencyColors;

        [Header("Shop Offers")]
        [Description("Rareties of each daily offers")]
        [SerializeField] private List<ERarety> m_DailyOffersRareties;

        [Description("Special offers limited in time")]
        [SerializeField] private List<SShopData> m_SpecialOffers;

        [Description("list of each chests offers in the shop")]
        [SerializeField] private List<SShopData> m_BundleShopData;

        [Description("list of all golds pack offers")]
        [SerializeField] private List<SShopData> m_GoldsShopData;

        [Description("list of each gems offers in the shop")]
        [SerializeField] private List<SShopData> m_GemsShopData;

        [Description("list of each XP offers in the shop")]
        [SerializeField] private List<SShopData> m_XpShopData;

        [Description("list of price of each characters")]
        [SerializeField] private List<SRaretyPriceData> m_CharacterPrices;

        // ===============================================================================
        // Static Accessors
        private static ShopManagementData m_Instance;

        public static float FastUnlockChestPrice            => Instance.m_FastUnlockChestPrice;
        public static List<SCurrencyColor> CurrencyColors   => Instance.m_CurrencyColors;
        public static List<ERarety> DailyOffersRareties     => Instance.m_DailyOffersRareties;
        public static List<SShopData> SpecialOffers         => Instance.m_SpecialOffers;
        public static List<SShopData> BundleShopData        => Instance.m_BundleShopData;
        public static List<SShopData> GoldsShopData         => Instance.m_GoldsShopData;
        public static List<SShopData> XpShopData            => Instance.m_XpShopData;
        public static List<SShopData> GemsShopData          => Instance.m_GemsShopData;

        #endregion


        #region Accessors

        public static ShopManagementData Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = AssetLoader.Load<ShopManagementData>("ShopManagementData", AssetLoader.c_ManagementDataPath);
                }

                return m_Instance;
            }
        }

        public static Color GetCurrencyColor(ECurrency currency)
        {
            foreach (SCurrencyColor currencyColor in CurrencyColors)
            {
                if (currencyColor.Currency == currency)
                    return currencyColor.Color;
            }

            ErrorHandler.Error("Unable to find CurrencyColor config for currency " + currency);
            return Color.white;
        }

        public static SPriceData GetPrice(Enum collectable)
        {
            ERarety rarety = CollectablesManagementData.GetData(collectable, 1, destroy: true).Rarety;
            foreach (var data in Instance.m_CharacterPrices)
            {
                if (data.Rarety == rarety)
                    return data.Price;
            }

            ErrorHandler.Error("No price data found for collectable " + collectable + " of rarety " + rarety);
            return new SPriceData(0, ECurrency.Golds);
        }

        #endregion
    }
}