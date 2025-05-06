using Data;
using Data.GameManagement;
using Enums;
using Game.AI.BehaviorTrees;
using Game.Loaders;
using Inventory;
using MyBox;
using Save;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Managers.Bots
{
    public static class BotBuilder
    {
        #region Members

        #endregion


        #region Generation

        /// <summary>
        /// Main entry method to create a bot based on player league data
        /// </summary>
        public static SPlayerData GenerateBot(SLeagueCloudData playerLeagueData)
        {
            // Step 2: Generate bot "playtime" and resources
            float botPlaytimeHours = GenerateBotPlaytime(playerLeagueData.CurrentLeague);
            int botXp, botGold;
            AllocateBotResources(botPlaytimeHours, out botXp, out botGold);

            // Step 3: Select character based on gold
            List<ECharacter> availableCharacters = GetAvailableCharacters(botGold);
            ECharacter character = SelectCharacter(availableCharacters);

            // Step 4: Open chests/power orbs to get pool of spells and runes
            GenerateSpellsAndRunes(botPlaytimeHours, out List<SpellData> availableSpells, out List<RuneData> availableRunes);

            // Step 5: Determine build style (mono or bi-element)
            List<ESpellElement> buildElements = SelectBuildElements(CharacterLoader.GetCharacterData(character).SpellElements);

            // Step 6: Filter and select spells and runes based on selected elements
            (ESpell[] spells, int[] spellLevels) = SelectSpellsForBuild(availableSpells, buildElements);
            (ERune[] runes, int[] runeLevels) = SelectRunesForBuild(availableRunes, buildElements);

            // Step 7: Determine character level based on XP
            int accountLevel = CalculateAccountLevel(botXp);

            // init Achievement Rewards based on league, hours played and account level
            AchievementGenerator.Generate(playerLeagueData.CurrentLeague, botPlaytimeHours, accountLevel);
            var playerName = PseudoGenerator.GeneratePseudo();
            return new SPlayerData(
                playerName: playerName,
                characterLevel: accountLevel,
                character: character.ToString(),
                runes: runes,
                runeLevels: runeLevels,
                spells: spells,
                spellLevels: spellLevels,
                profileData: new SProfileCurrentData(
                    accountLevel:   accountLevel,
                    gamerTag:       playerName,
                    avatar:         AchievementGenerator.Avatar,
                    border:         AchievementGenerator.Border,
                    title:          AchievementGenerator.Title,
                    badges:         AchievementGenerator.Badges
                ).AsNetworkSerializable(),
                isPlayer: false,
                botData: new SBotData(
                    difficulty:         playerLeagueData.CurrentLeague.ToString(), 
                    decisionRefresh:    CalculateDecisionRefresh(playerLeagueData.CurrentLeague), 
                    randomness:         CalculateRandomness(playerLeagueData.CurrentLeague), 
                    reactionTime:       CalculateReactionTime(playerLeagueData.CurrentLeague),
                    movementTime:       (0.1f, 1f),
                    movementRefresh:    (0.1f, 0.5f),
                    extraVariables:     GetExtraVariables(playerLeagueData.CurrentLeague)
                )
            );
        }

        static float CalculateDecisionRefresh(ELeague league)
        {
            return 0.35f;
        }

        static float CalculateRandomness(ELeague league)
        {
            return Random.Range(0f, 0.5f);
        }

        static (float, float) CalculateReactionTime(ELeague league)
        {
            float baseValue;
            switch (league)
            {
                case ELeague.Iron:
                    baseValue = 0.3f;
                    break;

                case ELeague.Bronze:
                    baseValue = Random.Range(0.2f, 0.3f);
                    break;

                case ELeague.Silver:
                    baseValue = Random.Range(0.15f, 0.25f);
                    break;

                case ELeague.Gold:
                    baseValue = Random.Range(0.15f, 0.2f);
                    break;

                case ELeague.Platinum:
                    baseValue = Random.Range(0.1f, 0.15f);
                    break;

                default:
                    baseValue = 0.1f;
                    break;
            }

            return (baseValue / 2,  baseValue * 2);
        }

        static Dictionary<string, float> GetExtraVariables(ELeague league)
        {
            switch (league)
            {
                case ELeague.Iron:
                    return new Dictionary<string, float>()
                    {
                        { EDefaultTreeVariables.AttackWeightBias.ToString(), 5f },
                        { EDefaultTreeVariables.DodgeWeightBias.ToString(), 0f },
                    };

                case ELeague.Bronze:
                case ELeague.Silver:
                    return new Dictionary<string, float>()
                    {
                        { EDefaultTreeVariables.AttackWeightBias.ToString(), Random.Range(3f, 5f)       },
                        { EDefaultTreeVariables.DodgeWeightBias.ToString(), Random.Range(0.2f, 0.5f)    },
                    };
  
                case ELeague.Gold:
                    return new Dictionary<string, float>()
                    {
                        { EDefaultTreeVariables.AttackWeightBias.ToString(), 3f },
                        { EDefaultTreeVariables.DodgeWeightBias.ToString(), 0.5f },
                    };

                default:
                    return new Dictionary<string, float>()
                    {
                        { EDefaultTreeVariables.AttackWeightBias.ToString(), 3f },
                        { EDefaultTreeVariables.DodgeWeightBias.ToString(), 0.2f },
                    };
            }
        }

        #endregion


        #region Step 2: Playtime and Resources

        static string GeneratePlayerName()
        {
            // TODO     
            return "RANDOM NAME";
        }

        static float GenerateBotPlaytime(ELeague league)
        {
            switch (league)
            {
                case ELeague.Iron:      return Random.Range(1f, 3f);
                case ELeague.Bronze:    return Random.Range(1f, 15f);
                case ELeague.Silver:    return Random.Range(5f, 40f);
                case ELeague.Gold:      return Random.Range(15f, 100f);
                case ELeague.Platinum:  return Random.Range(25f, 200f);
                default: return 500f;
            }
        }

        static void AllocateBotResources(float hours, out int xp, out int gold)
        {
            // TODO: Replace with real calculation based on statistics
            xp = Mathf.RoundToInt(hours * Random.Range(80, 120));
            gold = Mathf.RoundToInt(hours * Random.Range(50, 100));
        }

        #endregion


        #region Step 3: Character Selection

        static List<ECharacter> GetAvailableCharacters(int golds)
        {
            List<ECharacter> characters = new List<ECharacter>() { ECharacter.Alexander };
            foreach (ECharacter character in System.Enum.GetValues(typeof(ECharacter))) 
            {
                // TODO : with Randomness

                if (characters.Contains(character) || character == ECharacter.None)
                    continue;
                
                characters.Add(character);
            }

            return characters;
        }

        static ECharacter SelectCharacter(List<ECharacter> availableCharacters)
        {
            // TODO: Improve with weighted logic
            return availableCharacters[Random.Range(0, availableCharacters.Count)];
        }

        #endregion


        #region Step 4: Generate Spells and Runes

        static void GenerateSpellsAndRunes(float hours, out List<SpellData> spells, out List<RuneData> runes)
        {
            Dictionary<ESpell, int> spellsQty   = new();
            Dictionary<ERune, int> runesQty     = new();

            spells = new List<SpellData>();
            runes = new List<RuneData>();

            // generate a list of random chests that bot would have unlock in "X" hours
            List<EChest> chests = GenerateRandomChests(hours);

            // open each chests to collect pool of available spells and runes
            foreach (EChest chest in chests) 
            {
                List<SReward> chestRewards = ItemLoader.GetChestRewardData(chest).GenerateRewards();
                foreach (SReward reward in chestRewards)
                {
                    if (reward.RewardType == typeof(ESpell))
                    {
                        if (!System.Enum.TryParse(reward.RewardName, out ESpell spell))
                        {
                            ErrorHandler.Error("Unable to parse " + reward.RewardName + " as Spell");
                            continue;
                        }

                        if (! spellsQty.ContainsKey(spell))
                            spellsQty.Add(spell, reward.Qty);
                        else
                            spellsQty[spell] += reward.Qty;
                    }

                    else if (reward.RewardType == typeof(ERune))
                    {
                        if (!System.Enum.TryParse(reward.RewardName, out ERune rune))
                        {
                            ErrorHandler.Error("Unable to parse " + reward.RewardName + " as Rune");
                            continue;
                        }

                        if (! runesQty.ContainsKey(rune))
                            runesQty.Add(rune, reward.Qty);
                        else
                            runesQty[rune] += reward.Qty;
                    }
                }
            }

            // level up all runes and spells
            spells = UpgradeCollectables<SpellData>(spellsQty.ToDictionary(k => (System.Enum)k.Key, v => v.Value));
            runes = UpgradeCollectables<RuneData>(runesQty.ToDictionary(k => (System.Enum)k.Key, v => v.Value));
        }

        static List<T> UpgradeCollectables<T>(Dictionary<System.Enum, int> collectablesQty) where T : CollectableData
        {
            var returnedData = new List<T>();

            foreach (var collectable in collectablesQty)
            {
                CollectableData data = null;

                // Load correct data type based on T
                if (typeof(T) == typeof(SpellData) && collectable.Key is ESpell spell)
                {
                    data = SpellLoader.GetSpellData(spell);
                }
                else if (typeof(T) == typeof(RuneData) && collectable.Key is ERune rune)
                {
                    data = SpellLoader.GetRuneData(rune);
                }
                else
                {
                    Debug.LogError($"Invalid type or enum provided for upgrading: {collectable.Key}");
                    continue; // skip invalid entries
                }

                ERarety rarety = data.Rarety;
                int currentLevel = CollectablesManagementData.GetStartLevel(collectable.Key);
                int qty = collectable.Value;

                // Perform upgrade logic
                while (true)
                {
                    SLevelData levelData = CollectablesManagementData.GetSpellLevelData(currentLevel, rarety);
                    if (qty < levelData.RequiredQty)
                        break;

                    currentLevel++;
                    qty -= levelData.RequiredQty;
                }

                data.SetLevel(currentLevel);

                // Safely cast the data back to T
                returnedData.Add(data as T);
            }

            return returnedData;
        }

        static List<EChest> GenerateRandomChests(float hours)
        {
            // init list of chests that will be returned
            var chests = new List<EChest>();

            // quantity of power collected by a player in X hours
            var power = hours * Random.Range(1000f, 5000f);

            // amount power that a gem is worth
            var gemPower = SPowerOrb.RewardsPrice[ECurrency.Gems.ToString()];

            // shuffle chest types
            var chestsWeights = new Dictionary<EChest, int>()
            {
                { EChest.Common,    1   },
                { EChest.Rare,      3   },
                { EChest.Epic,      20  },
                { EChest.Legendary, 100 },
            };

            while (true) 
            {
                // select a weight-biased chest
                EChest chestType = GetRandomChest(chestsWeights);

                // get value (in power) of the expected chest
                var chestsValue = gemPower * ShopManagementData.BundleShopData.First(t => t.Rewards.Count == 1 && t.Rewards.Chests.Count == 1 && t.Rewards.Chests[0] == chestType).Cost;
                
                // if not enough power - remove the chest from available chests and select another one
                if (power < chestsValue)
                {
                    chestsWeights.Remove(chestType);

                    // not enough power for any chest - exit 
                    if (!chestsWeights.Any()) 
                        break;

                    continue;
                }

                // consume remaining power
                power -= chestsValue;

                // add chest to list of chests
                chests.Add(chestType);
            }

            return chests;
        }

        static EChest GetRandomChest(Dictionary<EChest, int> chestsWeights)
        {
            int totalWeight = chestsWeights.Values.Sum();
            int randomWeight = Random.Range(0, totalWeight);

            foreach (var chest in chestsWeights)
            {
                if (randomWeight < chest.Value)
                    return chest.Key;

                randomWeight -= chest.Value;
            }

            return chestsWeights.Keys.First(); // Fallback, should never be reached
        }

        #endregion


        #region Step 5: Build Style Selection

        static List<ESpellElement> SelectBuildElements(List<ESpellElement> characterElements)
        {
            List<ESpellElement> buildElements = new List<ESpellElement>
            {
                characterElements.Count > 0 ? characterElements[0] : ESpellElement.Neutral
            };

            // TODO: Possibly add a second element

            return buildElements;
        }

        #endregion


        #region Step 6: Select Spells and Runes

        static (ESpell[], int[]) SelectSpellsForBuild(List<SpellData> availableSpells, List<ESpellElement> elements)
        {
            var filteredSpells = availableSpells;
            SpellLoader.FilterByElement(ref filteredSpells, spellElementFilters: elements);

            // add random spells if not enough
            while (filteredSpells.Count < 4) 
            {
                var randomSpell = availableSpells.GetRandom();
                if (filteredSpells.Contains(randomSpell))
                    continue;

                filteredSpells.Add(randomSpell);
            }

            // select 4 random spells
            if (filteredSpells.Count > 4)
            {
                filteredSpells.Shuffle();
                filteredSpells = filteredSpells.GetRange(0, 4);
            }

            return (filteredSpells.Select(spell => spell.Spell).ToArray(), filteredSpells.Select(spell => spell.Level).ToArray());
        }

        static (ERune[], int[]) SelectRunesForBuild(List<RuneData> availableRunes, List<ESpellElement> elements)
        {
            var filteredRunes = availableRunes;
            SpellLoader.FilterByElement(ref filteredRunes, spellElementFilters: elements);

            // add random runes if not enough
            while (filteredRunes.Count < 3 && availableRunes.Count != filteredRunes.Count)
            {
                var randomRune = availableRunes.GetRandom();
                if (filteredRunes.Contains(randomRune))
                    continue;

                filteredRunes.Add(randomRune);
            }

            // select 3 random runes
            if (filteredRunes.Count > 3)
            {
                filteredRunes.Shuffle();
                filteredRunes = filteredRunes.GetRange(0, System.Math.Min(filteredRunes.Count, 3));
            }

            // fill missing with None rune
            var runes = filteredRunes.Select(rune => rune.Rune).ToList();
            var levels = filteredRunes.Select(spell => spell.Level).ToList();
            while (runes.Count() < 3)
            {
                runes.Add(ERune.None);
                levels.Add(1);
            }

            return (runes.ToArray(), levels.ToArray());
        }

        #endregion


        #region Step 7: Level Calculation

        static int CalculateAccountLevel(int xp)
        {
            int currentLevel = 1;
            while (xp > CollectablesManagementData.Instance.AccountLevelData[currentLevel - 1].RequiredXp)
            {
                xp -= CollectablesManagementData.Instance.AccountLevelData[currentLevel - 1].RequiredXp;
                currentLevel++;
            }

            return currentLevel;
        }

        #endregion
    }
}