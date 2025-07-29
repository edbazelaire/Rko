using Data;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;


namespace Menu.Common.Displayers
{
    public class LootInfoSidebar : MObject
    {
        #region Members

        // ===================================================================================
        // Data
        List<EChest> m_Chests;
        int m_ExtraGolds;

        // ===================================================================================
        // GameObjects & Components
        TMP_Text    m_Title;

        LootInfoRow m_GoldsContainer;
        LootInfoRow m_RuneContainer;
        LootInfoRow m_SpellsContainer;

        LootInfoRow m_CommonSpellsContainer;
        LootInfoRow m_RareSpellsContainer;
        LootInfoRow m_EpicSpellsContainer;
        LootInfoRow m_LegendarySpellsContainer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Title = Finder.FindComponent<TMP_Text>(gameObject, "Title");

            m_GoldsContainer            = Finder.FindComponent<LootInfoRow>(gameObject, "GoldsContainer");
            m_RuneContainer             = Finder.FindComponent<LootInfoRow>(gameObject, "RuneContainer");
            m_SpellsContainer           = Finder.FindComponent<LootInfoRow>(gameObject, "SpellsContainer");

            m_CommonSpellsContainer     = Finder.FindComponent<LootInfoRow>(gameObject, "CommonSpellsContainer");
            m_RareSpellsContainer       = Finder.FindComponent<LootInfoRow>(gameObject, "RareSpellsContainer");
            m_EpicSpellsContainer       = Finder.FindComponent<LootInfoRow>(gameObject, "EpicSpellsContainer");
            m_LegendarySpellsContainer  = Finder.FindComponent<LootInfoRow>(gameObject, "LegendarySpellsContainer");
        }

        public void Initialize(string title, List<EChest> chests, int extraGolds)
        {
            m_Chests = chests;
            m_ExtraGolds = extraGolds;

            base.Initialize();

            m_Title.text = title;
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // -------------------------------------------------------
            // Calculate all rewards
            int minGolds = m_ExtraGolds;
            int maxGolds = m_ExtraGolds;
            int totalSpells = 0;
            string rune = "";
            Dictionary<ERarety, int> spells = new()
            {
                { ERarety.Common,       0 },
                { ERarety.Rare,         0 },
                { ERarety.Epic,         0 },
                { ERarety.Legendary,    0 },
            };

            foreach (EChest chest in m_Chests)
            {
                ChestRewardData rewards = ItemLoader.GetChestRewardData(chest);
                var golds = rewards.Currencies.FirstOrDefault(t => t.Currency == ECurrency.Gold);
                minGolds += golds.Min;
                maxGolds += golds.Max;

                totalSpells += rewards.ExtraCardData.ExtraCards;
                foreach (var item in rewards.SpellsDistribution)
                {
                    totalSpells += item.Qty;
                    spells[item.Rarety] += item.Qty;
                }

                rune = rewards.RuneRarety.ToString();
            }

            if (m_Chests.Count > 1)
                rune = "x" + m_Chests.Count;

            // -------------------------------------------------------
            // Display rewards
            m_GoldsContainer.Initialize($"{minGolds} - {maxGolds}");
            m_RuneContainer.Initialize($"{rune}");
            m_SpellsContainer.Initialize($"x{totalSpells}");

            m_CommonSpellsContainer.Initialize($"x{spells[ERarety.Common]}");
            m_RareSpellsContainer.Initialize($"x{spells[ERarety.Rare]}");
            m_EpicSpellsContainer.Initialize($"x{spells[ERarety.Epic]}");
            m_LegendarySpellsContainer.Initialize($"x{spells[ERarety.Legendary]}");
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
