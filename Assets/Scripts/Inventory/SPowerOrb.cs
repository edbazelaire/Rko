using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Data;
using Enums;
using Game.Loaders;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;


namespace Inventory
{
    public class SPowerOrb
    {
        #region Members

        public const int    MAX_STARS                   = 5;
        public const float  BASE_STAR_UPGRADE_PERC      = 0.1f;
        public const int    FIX_STAR_BONUS              = 500;
        public const float  PERC_STAR_BONUS             = 0.3f;

        protected int m_Power;
        protected ERarety m_Rarety;

        public int      Power               => m_Power;
        public ERarety  Rarety              => m_Rarety;
        protected int   FinalPower          => (int)Math.Round(
            m_Power * Mathf.Pow(1 + PERC_STAR_BONUS, (int)m_Rarety)     // percentage bonus from rarety
            + FIX_STAR_BONUS * Mathf.Pow(2, (int)m_Rarety)              // fix value bonus from rarety
        );


        /// <summary>
        /// Associate Reward with its price in "Power"
        /// </summary>
        Dictionary<string, int> RewardsPrice = new Dictionary<string, int>()
        {
            // Currencies
            { ECurrency.Golds.ToString(),                   1       },
            { ECurrency.Xp.ToString(),                      3       },
            { ECurrency.Gems.ToString(),                    10      },

            // Spells
            { "Spell_" + ERarety.Common.ToString(),         25      },
            { "Spell_" + ERarety.Rare.ToString(),           75      },
            { "Spell_" + ERarety.Epic.ToString(),           500     },
            { "Spell_" + ERarety.Legendary.ToString(),      2500    },

            // Runes
            { "Rune_" + ERarety.Common.ToString(),          75      },
            { "Rune_" + ERarety.Rare.ToString(),            225     },
            { "Rune_" + ERarety.Epic.ToString(),            1500    },
            { "Rune_" + ERarety.Legendary.ToString(),       7500    },

            // Characters
            { "Character_" + ERarety.Common.ToString(),     250     },
            { "Character_" + ERarety.Rare.ToString(),       750     },
            { "Character_" + ERarety.Epic.ToString(),       5000    },
            { "Character_" + ERarety.Legendary.ToString(),  25000   },
        };

        #endregion


        #region Init & End

        public SPowerOrb(int power = 0, ERarety rarety = 0)
        {
            m_Power = power;
            m_Rarety = rarety;
        }

        public SPowerOrb(string name)
        {
            FromName(name);
        }

        #endregion


        #region Conversion

        public string ToName() => "PowerOrb_" + m_Power + "_" + m_Rarety.ToString();

        public void FromName(string name)
        {
            var values = name.Split('_');

            if (! Enum.TryParse(values[2], out ERarety rarety))
            {
                ErrorHandler.Error("Unable to parse " + name + " into Power/Rarety");
            }

            SetPower(int.Parse(values[1]));
            SetRarety(rarety);
        }

        public SReward AsReward()
        {
            return new SReward(typeof(EPowerOrb), ToName(), 1);
        }

        #endregion


        #region Rarety

        public void SetPower(int power)
        {
            m_Power = power;
        }

        public void AddPower(int power)
        {
            m_Power += power;
        }

        public void RemovePower(int power)
        {
            m_Power -= power;
        }

        public void SetRarety(ERarety rarety)
        {
            m_Rarety = rarety;
        }

        /// <summary>
        /// Try to upgrade star level of this orb. Return success
        /// </summary>
        /// <returns></returns>
        public bool TryUpgradeRarety()
        {
            // check errors
            if (m_Rarety < 0)
            {
                ErrorHandler.Error("NStars < 0 : " +  m_Rarety);
                return false;
            }

            // check stars not already maxed
            if (m_Rarety >= ERarety.Legendary)
                return false;

            // try percentage upgrade
            float perc = Mathf.Pow(BASE_STAR_UPGRADE_PERC, (int)m_Rarety + 1);
            if (UnityEngine.Random.Range(0f, 1f) > perc)
                return false;

            // upgrade and return success
            m_Rarety++;
            return true;
        }

        #endregion


        #region Rewards

        public List<SReward> GenerateRewards()
        {
            // Initialize the rewards list
            List<SReward> rewards = new List<SReward>();

            // Remaining power and items count
            int remainingPower = FinalPower;
            int garbagePower = 0;
            int nItems = 0;

            // Number of items to distribute power among
            const int maxItems = 6;

            // Maximum power bias factor (for creating non-linear distribution)
            const float powerBiasFactor = 2.0f;

            // Define probabilities for each item
            float[] probabilities = { 0.5f, 0.25f, 0.1f, 0.075f, 0.05f, 0.025f };

            // Normalize probabilities
            float totalProbability = probabilities.Sum();
            probabilities = probabilities.Select(p => p / totalProbability).ToArray();

            // Generate random power values for each item
            while (remainingPower >= FinalPower / 10 && nItems < maxItems)
            {
                // Determine the power split using a weighted random choice
                int basePower = FinalPower / maxItems;
                float randomValue = UnityEngine.Random.Range(0f, 1f);

                // Determine the multiplier for this item's power based on probabilities
                float powerMultiplier = 1.0f;
                float cumulativeProbability = 0.0f;

                for (int i = 0; i < probabilities.Length; i++)
                {
                    cumulativeProbability += probabilities[i];
                    if (randomValue <= cumulativeProbability)
                    {
                        powerMultiplier += i / powerBiasFactor;
                        break;
                    }
                }

                // Compute the power for this item
                int itemPower = Mathf.Clamp((int)(basePower * powerMultiplier), 1, remainingPower);

                // Create the reward
                SReward reward = CreateReward(itemPower);
                if (reward.Qty > 0)
                {
                    rewards.Add(reward);
                    remainingPower -= itemPower;
                    nItems++;
                }
            }

            // Assign remaining power to garbage (gold or XP)
            garbagePower = remainingPower;
            if (garbagePower > 0)
            {
                rewards.Add(new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), garbagePower));
            }

            return rewards;
        }

        /// <summary>
        /// Creates an SReward based on the provided power.
        /// </summary>
        /// <param name="power">Power value to determine the reward type and quantity.</param>
        /// <returns>An SReward instance.</returns>
        private SReward CreateReward(int power)
        {
            // Filter rewards within the acceptable power range
            var eligibleRewards = RewardsPrice.Where(kv => kv.Value <= power).ToList();

            if (eligibleRewards.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, eligibleRewards.Count);
                var selectedReward = eligibleRewards[randomIndex];

                string rewardName = selectedReward.Key;
                int rewardBasePower = selectedReward.Value;

                if (rewardName.StartsWith("Spell"))
                {
                    var rarity = GetRarityFromRewardName(rewardName);
                    SpellData spellData = SpellLoader.GetRandomSpell(new List<ERarety> { rarity });
                    if (spellData != null)
                    {
                        int qty = rarity == ERarety.Legendary ? 1 : Mathf.Max(1, power / rewardBasePower);
                        return new SReward(typeof(ESpell), spellData.Spell.ToString(), qty);
                    }
                }
                else if (rewardName.StartsWith("Rune"))
                {
                    var rarity = GetRarityFromRewardName(rewardName);
                    RuneData runeData = SpellLoader.GetRandomRune(new List<ERarety> { rarity });
                    if (runeData != null)
                    {
                        int qty = rarity == ERarety.Legendary ? 1 : Mathf.Max(1, power / rewardBasePower);
                        return new SReward(typeof(ERune), runeData.Rune.ToString(), qty);
                    }
                }
                else if (rewardName.StartsWith("Character"))
                {
                    var rarity = GetRarityFromRewardName(rewardName);
                    CharacterData characterData = CharacterLoader.GetRandomCharacter(new List<ERarety> { rarity }, null, false);
                    if (characterData != null)
                    {
                        return new SReward(typeof(ECharacter), characterData.Character.ToString(), 1);
                    }
                    else
                    {
                        // If no characters are available, fallback to another reward
                        return CreateFallbackReward(power);
                    }
                }
                else if (rewardName.StartsWith(ECurrency.Golds.ToString()) || rewardName.StartsWith(ECurrency.Xp.ToString()))
                {
                    int qty = Mathf.Max(1, power / rewardBasePower);
                    return new SReward(typeof(ECurrency), rewardName, qty);
                }
            }

            // Default to golds if no matches
            return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), 0);
        }

        /// <summary>
        /// Fallback reward in case primary selection fails.
        /// </summary>
        private SReward CreateFallbackReward(int power)
        {
            int qty = Mathf.Max(1, power);
            return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), qty);
        }

        /// <summary>
        /// Extracts the rarity from the reward name.
        /// </summary>
        /// <param name="rewardName">The name of the reward.</param>
        /// <returns>The rarity of the reward.</returns>
        private ERarety GetRarityFromRewardName(string rewardName)
        {
            foreach (ERarety rarity in Enum.GetValues(typeof(ERarety)))
            {
                if (rewardName.Contains(rarity.ToString()))
                {
                    return rarity;
                }
            }
            return ERarety.Common;
        }

        #endregion


        #region Loading 

        public PowerOrbUI LoadTemplate()
        {
            return AssetLoader.LoadPowerOrbTemplate(m_Rarety);
        }

        #endregion
    }
}