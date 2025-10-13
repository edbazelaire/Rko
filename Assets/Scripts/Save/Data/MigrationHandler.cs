using System.Collections.Generic;
using System;
using UnityEditor;
using Unity.Services.CloudSave.Models;
using System.Linq;
using Enums;
using Tools;
using Save.Data.Progression.Structs;

namespace Save.Data
{
    public static class MigrationHandler
    {
        #region Migrations

        public static Dictionary<string, Func<Item, object>> Migrations = new Dictionary<string, Func<Item, object>>
        {
            {
                ProfileCloudData.KEY_ACHIEVEMENTS, (item) =>
                {
                    try
                    {
                        var oldDict = item.Value.GetAs<Dictionary<string, int>>();
                        if (oldDict != null)
                            return oldDict.Select(p => new SAchievementInfo { Id = p.Key, Count = 0, Index = p.Value }).ToList();
                    }
                    catch(Exception) {}

                    return item.Value.GetAs<List<SAchievementInfo>>();
                }
            },

            {
                ProgressionCloudData.KEY_CURRENT_ARENA, (item) =>
                {
                    try
                    {
                        return item.Value.GetAs<SCurrentArenaCloudData>();
                    }
                    catch(Exception) 
                    {
                        return new SCurrentArenaCloudData();
                    }
                }
            },

            {
                ProgressionCloudData.KEY_UNLOCKED_ARENAS, (item) =>
                {
                    try
                    {
                        var oldDict = item.Value.GetAs<Dictionary<string, Dictionary<string, int>>>();
                        if (oldDict != null)
                        {
                            var newDict = new Dictionary<EArenaType, EArenaDifficulty>();
                            foreach (var kvp in oldDict)
                            {
                                var arenaTypeKey = kvp.Key;
                                var arenaData = kvp.Value;

                                if (! Enum.TryParse(arenaTypeKey, out EArenaType arenaType))
                                {
                                    ErrorHandler.Warning($"Found ProgressionCloudData - {ProgressionCloudData.KEY_UNLOCKED_ARENAS} with OLD STRUCTURE : Dictionary<string, Dictionary<string, int>> - but unable to convert {arenaTypeKey} int EArenaType");
                                    continue;
                                }

                                if (! arenaData.ContainsKey("Difficulty"))
                                {
                                    ErrorHandler.Warning($"Found ProgressionCloudData - {ProgressionCloudData.KEY_UNLOCKED_ARENAS} with OLD STRUCTURE : Dictionary<string, Dictionary<string, int>> - but unable to find field Difficulty");
                                    continue;
                                }

                                if (! Enum.IsDefined(typeof(EArenaDifficulty), arenaData["Difficulty"]))
                                {
                                    ErrorHandler.Warning($"Found ProgressionCloudData - {ProgressionCloudData.KEY_UNLOCKED_ARENAS} with OLD STRUCTURE : Dictionary<string, Dictionary<string, int>> - but unable to convert arena difficulty with value ({arenaData["Difficulty"]})");
                                    continue;
                                }

                                newDict[arenaType] = (EArenaDifficulty)arenaData["Difficulty"];
                            }

                            return newDict;
                        }
                    }
                    catch (Exception) {}

                    return item.Value.GetAs<Dictionary<EArenaType, EArenaDifficulty>>();
                }
            },

            {
                InventoryCloudData.KEY_SPELLS, (item) =>
                {
                    try
                    {
                        var oldList = item.Value.GetAs<List<SCollectableCloudData>>();
                        if (oldList == null)
                            return new List<SCollectableCloudData>();

                        var newList = new List<SCollectableCloudData>();
                        foreach (SCollectableCloudData cloudData in oldList)
                        {
                            if (Enum.TryParse(cloudData.CollectableName, out ESpell spell))
                            {
                                newList.Add(cloudData);
                                continue;
                            }

                            if (CheckSpellNameMigration(cloudData, out SCollectableCloudData newCloudData))
                            {
                                newList.Add(newCloudData);
                                ErrorHandler.Warning($"Found spell not existing spell : " + cloudData.CollectableName + " - converting it into : " + newCloudData.CollectableName);
                                continue;
                            }

                            ErrorHandler.Warning($"Found spell with UN-MATCHED name : " + cloudData.CollectableName + " - removing it from the list of spells");
                        }

                        return newList;
                    }
                    catch (Exception) {}

                    return item.Value.GetAs<Dictionary<EArenaType, EArenaDifficulty>>();
                }
            },
        };

        public static bool TryGetValue(Item item, out object value)
        {
            value = null;
            if (! Migrations.TryGetValue(item.Key, out var migrateFunc))
                return false;

            value = migrateFunc(item);
            return true;
        }

        #endregion


        #region Handle SPELLS

        /// <summary>
        /// Check if the new Name is matching a new name.
        /// </summary>
        /// <param name="collectableCloudData"></param>
        /// <returns></returns>
        static bool CheckSpellNameMigration(SCollectableCloudData collectableCloudData, out SCollectableCloudData newCollectableCloudData)
        {
            newCollectableCloudData = collectableCloudData;
            switch (collectableCloudData.CollectableName)
            {
                case "Shardrot":
                    newCollectableCloudData.CollectableName = ESpell.ShardrotIceLance.ToString();
                    return true;

                default:
                    return false;
            }
        }

        #endregion
    }
}