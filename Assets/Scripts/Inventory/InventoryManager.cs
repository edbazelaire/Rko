using Analytics.Events;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using static UnityEngine.Rendering.DebugUI;

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
        /// <summary> event fired when a character gains xp </summary>
        public static       Action<int>             XpGainedEvent;

        // =================================================================================================
        // ACCESSORS
        public static       int                             Golds         => Convert.ToInt32(InventoryCloudData.Instance.Data[InventoryCloudData.KEY_GOLDS]);
        public static       ChestData[]                     Chests        => (ChestData[])ChestsCloudData.Instance.Data[ChestsCloudData.KEY_CHESTS];

        #endregion


        #region Currency Management

        public static int GetCurrency(ECurrency currency)
        {
            return (int)InventoryCloudData.Instance.Data[currency.ToString()];
        }

        public static void UpdateCurrency(ECurrency currency, int amount, string context)
        {
            var data = InventoryCloudData.Instance.Data;

            if (!data.ContainsKey(currency.ToString()))
            {
                ErrorHandler.Error("Currency " + currency + " not found in inventory cloud data");
                return;
            }

            var total = (int)data[currency.ToString()] + amount;
            if (total < 0)
            {
                ErrorHandler.Error($"Not enought {currency} ({(int)data[currency.ToString()]}) to spend ({amount})");
                return;
            }

            MAnalytics.SendEvent(new CurrencyEvent(currency, amount, context));
            InventoryCloudData.Instance.SetData(currency.ToString(), total);
        }

        /// <summary>
        /// Check if the cost of the item is inf to current amount of golds
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public static bool CanBuy(int cost, ECurrency currency = ECurrency.Golds)
        {
            return GetCurrency(currency) - cost >= 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public static bool Spend(int cost, ECurrency currency, string context)
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
            MAnalytics.SendEvent(new CurrencyEvent(currency, cost, context));

            // save new currency value in cloud data
            InventoryCloudData.Instance.SetData(currency.ToString(), GetCurrency(currency) - cost);
            
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

            // add xp to character and save
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
            SLevelData levelData = CollectablesManagementData.GetLevelData(collectable, data.Level);

            // UPGRADE : spend golds and cards to update the level
            if (! Spend(levelData.RequiredGolds, ECurrency.Golds, "Upgrade" + collectable.GetType()))
                return;

            data.AddQty(- levelData.RequiredQty);
            data.Level++;

            // SAVE : update cloud data
            InventoryCloudData.Instance.SetCollectable(data);

            // fire event of upgrade
            CollectableUpgradedEvent?.Invoke(collectable, data.Level);

            // =============================================================================
            // TODO : BETA TESTING : make characters have same levels
            //      -> REMOVE later if not use OR set global account level
            if (collectable.GetType() == typeof(ECharacter))
            {
                // get all other characters to level up
                foreach (ECharacter character in Enum.GetValues(typeof(ECharacter)))
                {
                    SCollectableCloudData charData = InventoryCloudData.Instance.GetCollectable(character);
                    charData.Level = data.Level;

                    // SAVE : update cloud data
                    InventoryCloudData.Instance.SetCollectable(charData);

                    // fire event of upgrade
                    CollectableUpgradedEvent?.Invoke(collectable, data.Level);
                }
            }
            // =============================================================================
        }

        public static bool CanUpgrade(Enum collectable)
        {
            // get data of this character (current xp, level)
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);
            // get level data (required xp, golds, ...)
            SLevelData levelData = CollectablesManagementData.GetLevelData(collectable, data.Level);

            if (IsMaxLevel(collectable))
                return false;

            if (data.GetQty() < levelData.RequiredQty)
                return false;

            if (!CanBuy(levelData.RequiredGolds, ECurrency.Golds))
                return false;

            return true;
        }

        public static bool IsMaxLevel(Enum collectable)
        {
            SCollectableCloudData data = InventoryCloudData.Instance.GetCollectable(collectable);
            return data.Level >= CollectablesManagementData.GetMaxLevel(collectable);
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
        } 

        #endregion
    }
}