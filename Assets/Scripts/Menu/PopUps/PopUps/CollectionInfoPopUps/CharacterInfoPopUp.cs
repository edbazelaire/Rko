using Data;
using Inventory;
using Menu.Common.Infos;
using Save;
using System;
using System.Collections.Generic;
using TMPro;
using Tools;

namespace Menu.PopUps
{
    public class CharacterInfoPopUp : CollectableInfoPopUp
    {
        #region Members

        // =========================================================================================
        // GameObjects & Components
        TMP_Text m_DescriptionText;

        // =========================================================================================
        // Dependent Members
        CharacterData m_CharacterData => m_Data as CharacterData;
        protected override bool m_CanUpgrade => base.m_CanUpgrade && m_Data.Level < ProfileCloudData.AccountLevel;


        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
            m_DescriptionText       = Finder.FindComponent<TMP_Text>(gameObject, "Description");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();
            SetUpDescription();
        }

        #endregion



        #region UIManipulators

        void SetUpDescription()
        {
            m_DescriptionText.text = m_CharacterData.GetDescription();
        }

        #endregion
    }
}