using Data.GameManagement;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Menu.MainMenu.ShopTab
{
    public class SpecialOfferUI : MObject
    {
        #region Members

        TemplateCardShopItemUI m_TemplateDailyOffer;
        GameObject m_DailySection;

        TemplateBundleItemUI m_TemplateBundleItemUI;
        GameObject m_BundlesSection;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TemplateDailyOffer = AssetLoader.LoadShopTemplateItem<TemplateCardShopItemUI>();
            m_DailySection = Finder.Find(gameObject, "DailySection");

            m_TemplateBundleItemUI = AssetLoader.LoadShopTemplateItem<TemplateBundleItemUI>("TemplateSpecialBundleItem");
            m_BundlesSection = Finder.Find(gameObject, "BundlesSection");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            RefreshUI();
        }

        #endregion


        protected void Update()
        {
            if (TimeCloudData.Instance.CheckTimeData())
                RefreshUI();
        }


        #region GUI Manipulators

        void RefreshUI()
        {
            InitializeDailyOffers();
            InitializeSpecialBundles();
        }

        private void InitializeDailyOffers()
        {
            UIHelper.CleanContent(m_DailySection);

            if (m_TemplateDailyOffer == null)
            {
                ErrorHandler.Error("m_TemplateDailyOffer is null");
                return;
            }

            // init random daily offers
            for (int index = 0; index < ShopManagementData.DailyOffersRareties.Count + 1; index++)
            {
                var template = Instantiate(m_TemplateDailyOffer, m_DailySection.transform).GetComponent<TemplateCardShopItemUI>();
                SShopData data = SpellShopDataFromCloudData(index, out STimeData? timeData);
                
                // set first as free
                if (index == 0)
                    data.Cost = 0;

                template.Initialize(data, timeData);
            } 
        }

        private void InitializeSpecialBundles()
        {
            // clean before spawning
            UIHelper.CleanContent(m_BundlesSection);

            if (m_TemplateBundleItemUI == null)
            {
                ErrorHandler.Warning("Unable to init special bundles, Template is null");
                return;
            }

            foreach(var bundleData in ShopManagementData.SpecialOffers)
            {
                // ============================================================
                // TODO REMOVE : skip $ currencies
                if (bundleData.Currency == ECurrency.Dollars)
                    continue;
                // ============================================================

                var template = Instantiate(m_TemplateBundleItemUI, m_BundlesSection.transform);
                STimeData? timeData = bundleData.MaxCollection > 0 ? TimeCloudData.GetSpecialShopOffer(bundleData.Name) : null;

                if (bundleData.MaxCollection > 0 && timeData == null)
                {
                    ErrorHandler.Warning("Unable to find time data for SPECIAL OFFER : " + bundleData.Name);
                    timeData = TimeCloudData.GenerateSpecialShopOffer(bundleData);
                }

                template.GetComponent<TemplateBundleItemUI>().Initialize(bundleData, timeData);
            }
        }

        #endregion


        #region Helpers

        SShopData SpellShopDataFromCloudData(int index, out STimeData? timeData)
        {
            timeData = TimeCloudData.GetTimeData(TimeCloudData.GetDailyShopId(index));
            ESpell spell = ESpell.None;

            // check if timeData is valid 
            bool isOk = true;
            if (! timeData.HasValue)
            {
                ErrorHandler.Warning("NULL TimeData for " + TimeCloudData.GetDailyShopId(index) + ". This sould not happen here - please fix early");
                isOk = false;
            } 
            else if (timeData.Value.IsExpired())
            {
                ErrorHandler.Warning("EXPIRED TimeData for " + TimeCloudData.GetDailyShopId(index) + ". This sould not happen here - please fix early");
                isOk = false;
            }
            else if (! Enum.TryParse(timeData.Value.MetaData, out spell))
            {
                ErrorHandler.Error("Unable to parse Metadata " + timeData.Value.MetaData + " into Spell for TimeData " + TimeCloudData.GetDailyShopId(index));
                isOk = false;
            }

            if (!isOk)
            {
                List<ESpell> usedSpells = new();
                timeData = TimeCloudData.GenerateNewDailyShopOffer(index, ref usedSpells);
                TimeCloudData.UpdateTimeData(timeData.Value);

                if (! Enum.TryParse(timeData.Value.MetaData, out spell))
                {
                    ErrorHandler.Error("Unable to RE-PARSE Metadata " + timeData.Value.MetaData + " into Spell for TimeData " + TimeCloudData.GetDailyShopId(index));
                    return default;
                }
            }

            int qty;
            int price;
            ERarety rarety = SpellLoader.GetSpellData(spell, destroy: true).Rarety;

            switch (rarety)
            {
                case ERarety.Common:
                    qty = 50;
                    price = 500;
                    break;

                case ERarety.Rare:
                    qty = 30;
                    price = 1500;
                    break;

                case ERarety.Epic:
                    qty = 5;
                    price = 5000;
                    break;

                case ERarety.Legendary:
                    qty = 1;
                    price = 15000;
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + rarety);
                    return default;
            }

            var rewards = new SRewardsData();
            rewards.SetDefaultData();
            rewards.Add(spell, qty);

            return new SShopData(
                name:           "",
                icon:           AssetLoader.LoadIcon(spell),
                rewards:        rewards,
                currency:       ECurrency.Golds,
                cost:           price,
                maxCollection:  1
            );
        }

        #endregion

    }
}