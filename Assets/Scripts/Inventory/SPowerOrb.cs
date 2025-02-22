using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
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
        public const int    FIX_STAR_BONUS              = 5000;
        public const float  PERC_STAR_BONUS             = 1f;

        protected int m_Power;
        protected ERarety m_Rarety;

        public int      Power               => m_Power;
        public ERarety  Rarety              => m_Rarety;
        protected int   FinalPower          => (int)Math.Round(
            m_Power * Mathf.Pow(1 + PERC_STAR_BONUS, (int)m_Rarety)     // percentage bonus from rarety
            + FIX_STAR_BONUS * (int)m_Rarety                            // fix value bonus from rarety
        );

        /// <summary>
        /// Associate Reward with its price in "Power"
        /// </summary>
        Dictionary<string, int> RewardsPrice = new Dictionary<string, int>()
        {
            // Currencies
            { ECurrency.Golds.ToString(),                   1       },
            { ECurrency.Xp.ToString(),                      5       },
            { ECurrency.Gems.ToString(),                    50      },

            // Spells
            { "Spell_" + ERarety.Common.ToString(),         50      },
            { "Spell_" + ERarety.Rare.ToString(),           150     },
            { "Spell_" + ERarety.Epic.ToString(),           1500    },
            { "Spell_" + ERarety.Legendary.ToString(),      7500    },

            // Runes
            { "Rune_" + ERarety.Common.ToString(),          150     },
            { "Rune_" + ERarety.Rare.ToString(),            300     },
            { "Rune_" + ERarety.Epic.ToString(),            3000    },
            { "Rune_" + ERarety.Legendary.ToString(),       12000   },

            // Characters
            { "Character_" + ERarety.Common.ToString(),     500     },
            { "Character_" + ERarety.Rare.ToString(),       1500    },
            { "Character_" + ERarety.Epic.ToString(),       15000   },
            { "Character_" + ERarety.Legendary.ToString(),  50000   },
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

        /// <summary>
        /// Creates an SReward based on the provided power.
        /// </summary>
        /// <param name="power">Power value to determine the reward type and quantity.</param>
        /// <returns>An SReward instance.</returns>
        public List<SReward> GenerateRewards()
        {
            List<SReward> rewards = new List<SReward>();
            int basePower = FinalPower;
            int remainingPower = basePower;
            int nItems = 0;
            const int maxItems = 6;

            while (remainingPower > 0 && nItems < maxItems)
            {
                // Step 1: Calculate power for this item
                int itemPower = CalculateItemPower(basePower, remainingPower);

                // Step 2: Choose rarity based on RaretyPerc
                ERarety rarity = ChooseItemRarity();

                // Step 3: Choose reward type based on RewardsTypePercs
                ESubRewardType rewardType = ChooseRewardType();

                // Step 4: Create reward
                SReward reward = CreateReward(rewardType, itemPower, rarity);
                if (reward.Qty > 0)
                {
                    rewards.Add(reward);
                    nItems++;

                    // Deduct used power from remaining power
                    var name = rewardType == ESubRewardType.Currency ? reward.RewardName : rewardType + "_" + rarity;
                    int consumedPower = Mathf.Max(RewardsPrice[name] * reward.Qty, itemPower);
                    remainingPower -= consumedPower;
                }
            }

            // Add BONUS CURRENCY REWARD
            SReward bonusReward = CreateReward(ESubRewardType.Currency, Mathf.RoundToInt(FinalPower * UnityEngine.Random.Range(0.01f, 0.10f)), ChooseItemRarity());
            rewards.Add(bonusReward);

            return rewards;
        }

        /// <summary>
        /// Calculates the power to attribute to the next item.
        /// </summary>
        private int CalculateItemPower(int basePower, int remainingPower)
        {
            // Randomly attribute power from 10% to 30% of the remaining power
            return Mathf.Clamp(UnityEngine.Random.Range(basePower / 10, remainingPower / 3), 1, remainingPower);
        }

        /// <summary>
        /// Chooses a rarity based on the RaretyPerc distribution.
        /// </summary>
        private ERarety ChooseItemRarity()
        {
            // Retrieve the RaretyPercData array for the orb's rarity
            SRaretyPercData[] orbRaretyPercDataArray = LootManagementData.FindOrbRaretyPercData(m_Rarety);

            if (orbRaretyPercDataArray == null || orbRaretyPercDataArray.Length == 0)
            {
                ErrorHandler.Error("No rarity data found for the orb rarity: " + m_Rarety);
                return ERarety.Common; // Fallback to Common
            }

            // Extract percentages and normalize to sum to 1
            Dictionary<ERarety, float> currentRaretyPerc = new Dictionary<ERarety, float>();
            float total = 0f;

            foreach (var data in orbRaretyPercDataArray)
            {
                currentRaretyPerc[data.Rarety] = data.Percentage;
                total += data.Percentage;
            }

            if (total <= 0f)
            {
                ErrorHandler.Error("Total rarity percentage for orb rarity " + m_Rarety + " is invalid.");
                return ERarety.Common;
            }

            // Normalize percentages
            foreach (var key in currentRaretyPerc.Keys.ToList())
            {
                currentRaretyPerc[key] /= total;
            }

            // Perform weighted random selection
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            float cumulative = 0f;

            foreach (var pair in currentRaretyPerc)
            {
                cumulative += pair.Value;
                if (randomValue <= cumulative)
                {
                    return pair.Key;
                }
            }

            return ERarety.Common; // Fallback to Common
        }


        /// <summary>
        /// Chooses a reward type based on the RewardsTypePercs distribution.
        /// Ensures no Currency is selected for Legendary rarity.
        /// </summary>
        private ESubRewardType ChooseRewardType()
        {
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            float cumulative = 0;

            foreach (SSubRewardsTypePerc rewardTypePerc in LootManagementData.Instance.OrbSubRewardsTypePercs)
            {
                // Avoid currency as random reward type
                if (rewardTypePerc.SubRewardType == ESubRewardType.Currency)
                    continue;

                cumulative += rewardTypePerc.Percentage;
                if (randomValue <= cumulative)
                {
                    return rewardTypePerc.SubRewardType;
                }
            }

            return ESubRewardType.Currency; // Fallback to Currency if something goes wrong
        }

        /// <summary>
        /// Creates a reward based on the type, power, and rarity.
        /// </summary>
        private SReward CreateReward(ESubRewardType rewardType, int power, ERarety rarity)
        {
            switch (rewardType)
            {
                case ESubRewardType.Currency:
                    return CreateCurrencyReward(power, rarity);
                case ESubRewardType.Spell:
                    return CreateSpellReward(power, rarity);
                case ESubRewardType.Rune:
                    return CreateRuneReward(power, rarity);
                case ESubRewardType.Character:
                    return CreateCharacterReward(power, rarity);
                default:
                    return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), 0);
            }
        }

        /// <summary>
        /// Creates a currency reward.
        /// </summary>
        private SReward CreateCurrencyReward(int power, ERarety rarity)
        {
            string currencyName = rarity switch
            {
                ERarety.Common => ECurrency.Xp.ToString(),
                ERarety.Rare => ECurrency.Xp.ToString(),
                ERarety.Epic => ECurrency.Gems.ToString(),
                _ => null // Legendary should not result in a currency
            };

            if (currencyName == null)
                return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), 0);

            int qty = Mathf.Max(1, power / RewardsPrice[currencyName]);
            return new SReward(typeof(ECurrency), currencyName, qty);
        }

        /// <summary>
        /// Creates a spell reward.
        /// </summary>
        private SReward CreateSpellReward(int power, ERarety rarity)
        {
            var spellData = SpellLoader.GetRandomSpell(new List<ERarety> { rarity });
            if (spellData == null)
                return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), 0);

            int qty = rarity == ERarety.Legendary ? 1 : Mathf.Max(1, power / RewardsPrice["Spell_" + rarity]);
            return new SReward(typeof(ESpell), spellData.Spell.ToString(), qty);
        }

        /// <summary>
        /// Creates a rune reward.
        /// </summary>
        private SReward CreateRuneReward(int power, ERarety rarity)
        {
            var runeData = SpellLoader.GetRandomRune(new List<ERarety> { rarity });
            if (runeData == null)
                return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), 0);

            int qty = rarity == ERarety.Legendary ? 1 : Mathf.Max(1, power / RewardsPrice["Rune_" + rarity]);
            return new SReward(typeof(ERune), runeData.Rune.ToString(), qty);
        }

        /// <summary>
        /// Creates a character reward.
        /// </summary>
        private SReward CreateCharacterReward(int power, ERarety rarity)
        {
            var characterData = CharacterLoader.GetRandomCharacter(new List<ERarety> { rarity }, null, false);
            if (characterData == null)
                return CreateFallbackReward(power); // Fallback to another reward type if no characters are available

            return new SReward(typeof(ECharacter), characterData.Character.ToString(), 1);
        }

        /// <summary>
        /// Fallback reward in case a specific reward cannot be created.
        /// </summary>
        private SReward CreateFallbackReward(int power)
        {
            return new SReward(typeof(ECurrency), ECurrency.Golds.ToString(), Mathf.Max(1, power));
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