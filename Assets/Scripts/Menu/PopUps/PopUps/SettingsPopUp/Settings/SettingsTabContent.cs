using Data.GameManagement;
using Menu.MainMenu;
using System;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class SettingsTabContent : TabContent
    {
        #region Members

        GameObject m_TemplateSettingOption;

        GameObject  m_Content;
        Button m_ResetButton; 

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TemplateSettingOption = AssetLoader.Load<GameObject>("SettingOption", AssetLoader.c_SettingsPath);
            m_Content = Finder.Find(gameObject, "Content");
            m_ResetButton = Finder.FindComponent<Button>(gameObject, "ResetButton");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            UIHelper.CleanContent(m_Content);
            foreach (ESettings option in Enum.GetValues(typeof(ESettings)))
            {
                SettingOptionUI settingOptionUI = Instantiate(m_TemplateSettingOption, m_Content.transform).GetComponent<SettingOptionUI>();
                settingOptionUI.Initialize(option);
            }
        }

        #endregion


        #region GUI Manipulators

        public override void Activate(bool activate)
        {
            gameObject.SetActive(activate);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_ResetButton.onClick.AddListener(OnResetButton);
        }

        void OnResetButton()
        {
            Settings.Reload();
            SetUpUI();
        }

        #endregion
    }
}