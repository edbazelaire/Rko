using Enums;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.ComponentModel;
using Tools;
using Inventory;
using Save;
using Unity.VisualScripting;
using MyBox;
using Managers.Monetization.IAP;
using System.Linq;


namespace Data.GameManagement
{
    [Serializable]
    public struct SPriceData
    {
        public float        Price;
        public ECurrency    Currency;

        public SPriceData(float price, ECurrency currency)
        {
            Price       = price;
            Currency    = currency;
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
    public struct SBoostReward
    {
        public EBoost       Boost;
        public int          Duration;

        public SBoostReward(EBoost boost, int duration)
        {
            this.Boost = boost;
            this.Duration = duration;
        }

        public SReward AsReward()
        {
            return new SReward(typeof(EBoost), Boost.ToString(), Duration);
        }
    }

    [Serializable]
    public struct SRewardsData
    {
        public List<SCurrencyReward>        Currencies;
        public List<EChest>                 Chests;
        public List<SPowerOrb>              PowerOrbs;
        public List<SCollectableReward>     Collectables;
        public List<SAchievementReward>     AchievementRewards;
        public List<SBoostReward>           Boosts;

        public readonly bool IsEmpty => Count == 0;
        public readonly int Count => 
            (Currencies != null             ? Currencies.Count          : 0)
            + (Chests != null               ? Chests.Count              : 0)
            + (PowerOrbs != null            ? PowerOrbs.Count           : 0)
            + (Collectables != null         ? Collectables.Count        : 0)
            + (AchievementRewards != null   ? AchievementRewards.Count  : 0)
            + (Boosts != null               ? Boosts.Count              : 0);

        public SRewardsData(List<SCurrencyReward> currencyRewards       = null,
                            List<EChest> chests                         = null,
                            List<SPowerOrb> powerOrbs                   = null,
                            List<SCollectableReward> collectableRewards = null,
                            List<SAchievementReward> achievementRewards = null,
                            List<SBoostReward> boosts                   = null)
        {
            Currencies          = currencyRewards       ?? new List<SCurrencyReward>();
            Chests              = chests                ?? new List<EChest>();
            PowerOrbs           = powerOrbs             ?? new List<SPowerOrb>();
            Collectables        = collectableRewards    ?? new List<SCollectableReward>();
            AchievementRewards  = achievementRewards    ?? new List<SAchievementReward>();
            Boosts              = boosts                ?? new List<SBoostReward>();
        }

        public void SetDefaultData()
        {
            Currencies          ??= new List<SCurrencyReward>();
            Chests              ??= new List<EChest>();
            PowerOrbs           ??= new List<SPowerOrb>();
            Collectables        ??= new List<SCollectableReward>();
            AchievementRewards  ??= new List<SAchievementReward>();
            Boosts              ??= new List<SBoostReward>();
        }

        #region Adding Rewards

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
            
            else if (item.GetType() == typeof(EBoost))
            {
                Boosts ??= new List<SBoostReward>();
                Boosts.Add(new SBoostReward((EBoost)item, duration: qty));
            }

            else
                ErrorHandler.Error("Unhandled type of item " + item.GetType());
        }

        public void Add(SPowerOrb powerOrb)
        {
            PowerOrbs ??= new List<SPowerOrb>();
            PowerOrbs.Add(powerOrb);
        }

        public void Add(SAchievementReward achievementReward)
        {
            AchievementRewards ??= new List<SAchievementReward>();
            AchievementRewards.Add(achievementReward);
        }

        public void Add(List<SAchievementReward> achievementRewards)
        {
            AchievementRewards ??= new List<SAchievementReward>();
            AchievementRewards.AddRange(achievementRewards);
        }

        public void Add(SRewardsData rewardsData)
        {
            if (rewardsData.Currencies != null)
            {
                Currencies ??= new List<SCurrencyReward>();
                Currencies.AddRange(rewardsData.Currencies);
            }
            
            if (rewardsData.Chests != null)
            {
                Chests ??= new List<EChest>();
                Chests.AddRange(rewardsData.Chests);
            }

            if (rewardsData.PowerOrbs != null)
            {
                PowerOrbs ??= new List<SPowerOrb>();
                PowerOrbs.AddRange(rewardsData.PowerOrbs);
            }

            if (rewardsData.Collectables != null)
            {
                Collectables ??= new List<SCollectableReward>();
                Collectables.AddRange(rewardsData.Collectables);
            }

            if (rewardsData.AchievementRewards != null)
            {
                AchievementRewards ??= new List<SAchievementReward>();
                AchievementRewards.AddRange(rewardsData.AchievementRewards);
            }

            if (rewardsData.Boosts != null)
            {
                Boosts ??= new List<SBoostReward>();
                Boosts.AddRange(rewardsData.Boosts);
            }
        }

        #endregion


        #region Getters

        public int GetCurrency(ECurrency currency)
        {
            if (Currencies == null)
                return 0;

            int amount = 0;
            foreach (var value in Currencies.Where(t => t.Currency == currency))
            {
                amount += value.Qty;
            }

            return amount;
        }

        #endregion


        #region Formating Rewards

        public List<SReward> Rewards
        {
            get
            {
                List<SReward> list = AsRewardStruct(Currencies);
                list.AddRange(AsRewardStruct(Chests));
                list.AddRange(AsRewardStruct(PowerOrbs));
                list.AddRange(AsRewardStruct(Collectables));
                list.AddRange(AsRewardStruct(AchievementRewards));
                list.AddRange(AsRewardStruct(Boosts));

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
        public List<SReward> AsRewardStruct(List<SPowerOrb> powerOrbs)
        {
            if (powerOrbs == null || powerOrbs.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (SPowerOrb data in powerOrbs)
            {
                rewards.Add(data.AsReward());
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

        public List<SReward> AsRewardStruct(List<SBoostReward> boosts)
        {
            if (boosts == null || boosts.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (SBoostReward data in boosts)
            {
                rewards.Add(data.AsReward());
            }

            return rewards;
        }

        public List<SReward> AsRewardStruct(List<EEmot> emots)
        {
            if (emots == null || emots.Count == 0)
                return new List<SReward>();

            var rewards = new List<SReward>();
            foreach (EEmot data in emots)
            {
                rewards.Add(new SReward(typeof(EEmot), data.ToString(), 1));
            }

            return rewards;
        }

        #endregion
    }

    [Serializable]
    public struct SRaretyPriceData
    {
        public ERarety          Rarety;
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
        // if can be bought with real currency - link to the product id
        public EProduct Product;
        /// <summary> Name or Title of the product on the TemplateItem </summary>
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

        bool m_Abort;

        public bool Abort => m_Abort;
        public float Price => (1 - Reduction) * Cost;
        public string ProductId => !Name.IsNullOrEmpty() ? Name : (Rewards.Rewards[0].RewardName + (Rewards.Rewards[0].Qty > 0 ? "_" + Rewards.Rewards[0].Qty : ""));
        public string PrettyName {
            get
            {
                if (!Name.IsNullOrEmpty())
                    return Name;

                var reward = Rewards.Rewards[0];
                string rewardName = reward.RewardName;
                if (reward.RewardType == typeof(EChest))
                    rewardName += " Chest";
                if (reward.Qty > 1)
                    rewardName += " x" + Rewards.Rewards[0].Qty;

                return rewardName;
            }    
        }

        public SShopData(EProduct product, string name, Sprite icon, SRewardsData rewards, ECurrency currency, int cost, int maxCollection, float reduction = 0)
        {
            Product         = product;
            Name            = name;
            Icon            = icon;
            Rewards         = rewards;
            Currency        = currency;
            Cost            = cost;
            MaxCollection   = maxCollection;
            Reduction       = reduction;

            m_Abort = false;

            // check on init
            Check();
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

        public bool Check()
        {
            m_Abort = false;
            
            // CHECK : that a real currency product has Store ProductId
            if (Currency == ECurrency.Real)
            {
                if (Product == EProduct.None)
                {
                    ErrorHandler.Error($"Product {Name} has a {ECurrency.Real} currency but has no product identifier");
                    m_Abort = true;
                    return false;
                }

                if (!IAPManager.Initialized)
                    return false;

                var storeProduct = IAPManager.Instance.GetProduct(Product);
                if (storeProduct == null)
                {
                    ErrorHandler.Error($"Product {Product} not found");
                    m_Abort = true;
                    return false;
                }

                Cost = (float)storeProduct.metadata.localizedPrice;
            }

            // CHECK : Reduction
            if (Reduction < 0 || Reduction > 1)
            {
                ErrorHandler.Error("Trying to apply reduction (" + Reduction + ") not between 0 and 1 on " + Name);
                Reduction = 0;
            }

            // CHECK : price not negative
            if (Price < 0)
            {
                ErrorHandler.Error($"ShopItem {Name} has a price {Price} < 0");
                return false;
            }

            return true;
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

        [Description("list of each KEYS offers in the shop")]
        [SerializeField] private List<SShopData> m_KeysShopData;

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
        public static List<SShopData> KeysShopData          => Instance.m_KeysShopData;

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
            if (collectable is not ECharacter)
                return new SPriceData(0, ECurrency.Gold);

            ERarety rarety = CollectablesManagementData.GetData(collectable, 1, destroy: true).Rarety;
            foreach (var data in Instance.m_CharacterPrices)
            {
                if (data.Rarety == rarety)
                    return data.Price;
            }

            ErrorHandler.Error("No price data found for collectable " + collectable + " of rarety " + rarety);
            return new SPriceData(0, ECurrency.Gold);
        }

        #endregion
    }
}