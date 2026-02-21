using Analytics.Events;
using Assets.Scripts.Managers;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Save;
using System;
using Tools;

namespace Inventory
{
    public static class InventoryManager
    {
        #region Members

        // =================================================================================================
        // CONSTANTS
        public const        int                     MAX_CHESTS      = 4;

        // =================================================================================================
        // EVENTS
        public static       Action<ChestData, int>  ChestsAddedEvent;
        public static       Action<Enum>            UnlockCollectableEvent;

        /// <summary> event fired when a collectable has been upgraded (=level up) </summary>
        public static       Action<Enum, int>       CollectableUpgradedEvent;
        /// <summary> event fired when a collectable mastery has been upgraded </summary>
        public static       Action<Enum, int>       CollectableMasteryUpgradedEvent;
        /// <summary> event fired when a character gains xp </summary>
        public static       Action<int>             XpGainedEvent;

        // =================================================================================================
        // ACCESSORS
        public static       int                             Golds         => Convert.ToInt32(InventoryCloudData.Instance.Data[InventoryCloudData.KEY_GOLD]);
        public static       ChestData[]                     Chests        => (ChestData[])ChestsCloudData.Instance.Data[ChestsCloudData.KEY_CHESTS];

        #endregion


        #region Currency Management

        public static int GetCurrency(ECurrency currency)
        {
            return InventoryCloudData.Instance.GetCurrency(currency);
        }

        public static void UpdateCurrency(ECurrency currency, int amount, string context, bool save = true)
        {
            var total = InventoryCloudData.Instance.GetCurrency(currency) + amount;
            if (total < 0)
            {
                ErrorHandler.Error($"Not enought {currency} ({InventoryCloudData.Instance.GetCurrency(currency)}) to spend ({amount})");
                return;
            }

            if (amount > 0 && currency == ECurrency.Xp)
            {
                UpdateCurrency(ECurrency.TotalXp, amount, context, save);
            }

            MAnalytics.SendEvent(new CurrencyEvent(currency, amount, context));
            InventoryCloudData.Instance.SetData(currency.ToString(), total, save);
        }

        public static bool CanBuy(Enum collectable)
        {
            if (collectable is EBoss)
                return false;

            if (collectable is ESpawn)
                return false;

            SPriceData priceData = ShopManagementData.GetPrice(collectable);
            return CanBuy((int)priceData.Price, priceData.Currency);
        }

        /// <summary>
        /// Check if the cost of the item is inf to current amount of golds
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public static bool CanBuy(float cost, ECurrency currency = ECurrency.Gold)
        {
            return GetCurrency(currency) - cost >= 0;
        }


        public static bool Spend(SPriceData priceData, string context, bool save = true)
        {
            return Spend(priceData.Price, priceData.Currency, context, save);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public static bool Spend(float cost, ECurrency currency, string context, bool save = true)
        {
            if (cost < 0)
            {
                ErrorHandler.Error($"Trying to add a spend a negative amount of gold ({cost}) : use the AddGolds() method");
                return false;
            }

            if (!CanBuy(cost, currency))
            {
                ErrorHandler.Error($"Not enought {currency} ({GetCurrency(currency)}) to buy the item ({cost}) : this situation should not happen");
                return false;
            }

            // fire analytics event that the currency has been spent
            MAnalytics.SendEvent(new CurrencyEvent(currency, -(int)cost, context));

            // save new currency value in cloud data
            InventoryCloudData.Instance.SetData(currency.ToString(), GetCurrency(currency) - cost, save);
            
            return true;
        }

        #endregion


        #region Collectables Management

        /// <summary>
        /// Add xp to a character : check if raise a level up
        /// </summary>
        /// <param name="character"></param>
        /// <param name="qty"></param>
        public static void AddCollectable(Enum collectable, int qty)
        {
            if (qty < 0)
            {
                ErrorHandler.Error("Trying to provide negative qty (" + qty + ") to collectable " + collectable);
                return;
            }

            if (qty == 0)
            {
                ErrorHandler.Warning("Trying to provide 0 qty to collectable " + collectable);
                return;
            }

            // get data of this character (current xp, level)
            SCollectableCloudData collectableData = InventoryCloudData.Instance.GetCollectable(collectable);
            if (collectableData.Level == 0)
            {
                Unlock(ref collectableData);    // unlock the item
                qty -= 1;                       // consume 1 from the qty for the unlocking
            }

            // add qty to the collectable
            collectableData.AddQty(qty);
            InventoryCloudData.Instance.SetCollectable(collectableData);
        }

        /// <summary>
        /// Unlock a spell
        /// </summary>
        /// <param name="spellCloudData"></param>
        public static void Unlock(ref SCollectableCloudData collectableCloudData)
        {
            if (collectableCloudData.Level > 0)
            {
                ErrorHandler.Error("Trying to unlock spell " + collectableCloudData.GetCollectable() + " but already has level " + collectableCloudData.Level);
                return;
            }

            collectableCloudData.Level = CollectablesManagementData.GetStartLevel(collectableCloudData.GetCollectable());
            InventoryCloudData.Instance.SetCollectable(collectableCloudData);

            UnlockCollectableEvent?.Invoke(collectableCloudData.GetCollectable());
        }

        public static void Upgrade(Enum collectable)
        {
            if (! CanUpgrade(collectable))
            {
                ErrorHandler.Error("Trying to upgrade " + collectable + " but this action is not authorized - this should never happen, fix");
                return;
            }

            // get data of this character (current xp, level)
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);
            // get level data (required xp, golds, ...)
            SLevelData levelData = CollectablesManagementData.GetLevelData(collectable, data.Level, data.Mastery);

            // UPGRADE : spend golds and cards to update the level
            if (! Spend(levelData.RequiredGold, ECurrency.Gold, "Upgrade" + collectable.GetType().ToString().Replace("Enums.E", "") + "." + collectable.ToString()))
                return;

            data.AddQty(- levelData.RequiredQty);
            data.Level++;

            // SAVE : update cloud data
            InventoryCloudData.Instance.SetCollectable(data);

            // fire event of upgrade
            CollectableUpgradedEvent?.Invoke(collectable, data.Level);
        }

        public static bool UpgradeMastery(Enum collectable, int mastery)
        {
            // get data from cloud
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);

            // go to next mastery
            data.Mastery = mastery;

            // SAVE : update cloud data
            InventoryCloudData.Instance.SetCollectable(data);

            // fire event of upgrade
            CollectableMasteryUpgradedEvent?.Invoke(collectable, data.Mastery);

            // return that everything went fine
            return true;
        }

        public static bool CanUpgrade(Enum collectable)
        {
            // get data of this character (current xp, level)
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);
            // get level data (required xp, golds, ...)
            SLevelData levelData = CollectablesManagementData.GetLevelData(collectable, data.Level, data.Mastery);

            if (IsMaxLevel(collectable))
                return false;

            if (data.GetQty() < levelData.RequiredQty)
                return false;

            if (!CanBuy(levelData.RequiredGold, ECurrency.Gold))
                return false;

            return true;
        }

        public static bool CanUpgradeMastery(Enum collectable)
        {
            // get data of this character (current xp, level)
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);

            // get rarety of the collectable
            var rarety = CollectablesManagementData.GetData(collectable, 1).Rarety;

            // try get mastery upgrade data cost (required gems, golds, ...)
            if (! CollectablesManagementData.TryGetMasteryUpgradeCost(rarety, data.Mastery, out SPriceData price))
                return false;

            if (IsMaxMastery(collectable))
                return false;

            if (! CanBuy(price.Price, price.Currency))
                return false;

            return true;
        }

        public static bool IsMaxLevel(Enum collectable)
        {
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);
            return data.Level >= CollectablesManagementData.GetMaxLevel(collectable);
        }

        public static bool IsMaxMastery(Enum collectable)
        {
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);
            return data.Mastery >= CollectablesManagementData.MAX_MASTERY;
        }

        #endregion


        #region Spell Management

        /// <summary>
        /// Get data of a spell from cloud data
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public static SCollectableCloudData GetSpellData(ESpell spell)
        {
            return InventoryCloudData.Instance.GetSpell(spell);
        }

        #endregion


        #region Chests Management

        public static bool GetFirstAvailableIndex(out int index)
        {
            index = -1; 
            for (int i = 0; i < Chests.Length; i++)
            {
                if (Chests[i] == null)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public static ChestData CreateRandomChest()
        {
            Array values = Enum.GetValues(typeof(EChest));
            var random = new System.Random();
            return new ChestData((EChest)values.GetValue(random.Next(values.Length)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chestType"></param>
        public static void AddChest(EChest chestType) 
        {
            AddChest(new ChestData(chestType));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chestData"></param>
        public static void AddChest(ChestData chestData)
        {
            if (!GetFirstAvailableIndex(out int index))
                return;

            // add data to list of chest data
            Chests[index] = chestData;

            // fire event that a chest has been added
            ChestsAddedEvent?.Invoke(chestData, index);

            // call for async save of the updated value
            ChestsCloudData.Instance.SaveValue(ChestsCloudData.KEY_CHESTS);
            MobileNotificationManager.RefreshChestReadyNotification();
        }

        public static void RemoveChestAtIndex(int index)
        {
            // this can happen if the negative index check was not done before the call
            if (index < 0)
            {
                ErrorHandler.Warning("Trying to remove chest with index " + index + ". This should be handled before calling this method");
                return;
            }

            // this should never happen
            if (index >= Chests.Length)
            {
                ErrorHandler.Error("Trying to remove chest with " + index);
                return;
            }

            // remove chest data from cloud data
            Chests[index] = null;
            ChestsCloudData.Instance.SaveValue(ChestsCloudData.KEY_CHESTS);
            MobileNotificationManager.RefreshChestReadyNotification();
        } 

        #endregion
    }
}