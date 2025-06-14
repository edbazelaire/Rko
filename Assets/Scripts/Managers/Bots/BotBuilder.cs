using Data;
using Data.GameManagement;
using Enums;
using Game.AI.BehaviorTrees;
using Game.Loaders;
using Save;
using Save.Data.Progression.Structs;
using System.Collections.Generic;
using System.Linq;
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
            AllocateBotResources(botPlaytimeHours, out int botXp);

            // Step 3: Select a random character
            List<ECharacter> availableCharacters = GetAvailableCharacters();
            ECharacter character = SelectCharacter(availableCharacters);

            // Step 4: Determine build style (mono or bi-element)
            List<ESpellElement> buildElements = SelectBuildElements(CharacterLoader.GetCharacterData(character).SpellElements);

            // Step 5: Determine character level based on XP
            int accountLevel = CalculateAccountLevel(botXp);
            
            // Step 6: Filter and select spells and runes based on selected elements
            ESpell[] spells = SelectSpellsForBuild(buildElements);
            ERune[] runes = SelectRunesForBuild(buildElements);
            int[] spellLevels = Enumerable.Range(0, 4)
                .Select(_ => Mathf.Clamp(Random.Range(accountLevel - 1, accountLevel + 4), 0, 14))
                .ToArray();
            int[] runeLevels = Enumerable.Range(0, 4)
                .Select(_ => Mathf.Clamp(Random.Range(accountLevel - 1, accountLevel + 4), 0, 14))
                .ToArray();

            // init Achievement Rewards based on league, hours played and account level
            AchievementGenerator.Generate(playerLeagueData.CurrentLeague, botPlaytimeHours, accountLevel);
            var playerName = PseudoGenerator.GeneratePseudo();
            return new SPlayerData(
                playerName:     playerName,
                characterLevel: accountLevel,
                character:      character.ToString(),
                runes:          runes,
                runeLevels:     runeLevels,
                spells:         spells,
                spellLevels:    spellLevels,
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
                    movementTime:       CalculateMovementTime(playerLeagueData.CurrentLeague),
                    movementRefresh:    (0.6f, 1.5f),
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
            switch (league)
            {
                case ELeague.Iron:
                    return 0.5f;

                case ELeague.Bronze:
                    return Random.Range(0.2f, 0.5f);

                case ELeague.Silver:
                    return Random.Range(0.1f, 0.3f);

                default:
                    return 0f;
            }
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

                default:
                    baseValue = 0f;
                    break;
            }

            return (baseValue / 2,  baseValue * 2);
        }

        static (float, float) CalculateMovementTime(ELeague league)
        {
            switch (league)
            {
                case ELeague.Iron:
                    return (0.5f, 1f);

                case ELeague.Bronze:
                    return (0.2f, 0.8f);

                case ELeague.Silver:
                    return (0.1f, 0.5f);

                default:
                    return (0f, 0f);
            }
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
                        { EDefaultTreeVariables.AttackWeightBias.ToString(), 5f                         },
                        { EDefaultTreeVariables.DodgeWeightBias.ToString(), Random.Range(0.2f, 0.5f)    },
                    };

                default:
                    return new Dictionary<string, float>()
                    {
                        { EDefaultTreeVariables.AttackWeightBias.ToString(), 5f },
                        { EDefaultTreeVariables.DodgeWeightBias.ToString(), 0f },
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
                case ELeague.Silver:    return Random.Range(15, 50f);
                case ELeague.Gold:      return Random.Range(35f, 150f);
                case ELeague.Platinum:  return Random.Range(25f, 250f);
                default: return Random.Range(500f, 5000f);
            }
        }

        static void AllocateBotResources(float hours, out int xp)
        {
            // TODO: Replace with real calculation based on statistics
            xp = Mathf.RoundToInt(hours * Random.Range(120, 200));
        }

        #endregion


        #region Step 3: Character Selection

        static List<ECharacter> GetAvailableCharacters()
        {
            List<ECharacter> characters = new List<ECharacter>() { ECharacter.Alexander };
            foreach (ECharacter character in System.Enum.GetValues(typeof(ECharacter))) 
            {
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

        static ESpell[] SelectSpellsForBuild(List<ESpellElement> elements)
        {
            var filteredSpells = SpellLoader.FilterSpells(spellElementFilters: elements);
            filteredSpells.Shuffle();
            return filteredSpells.GetRange(0, 4).Select(spell => spell.Spell).ToArray();
        }

        static ERune[] SelectRunesForBuild(List<ESpellElement> elements)
        {
            var filteredRunes = SpellLoader.FilterRunes(elementsFilter: elements);
            filteredRunes.Shuffle();
            return filteredRunes.GetRange(0, System.Math.Min(filteredRunes.Count, 3)).Select(data => data.Rune).ToArray();
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