using UnityEngine;
using UnityEngine.UI;
using Tools;
using System;
using Data.ArenaEffects.ArenaMods;
using Menu.Common.Rewards;
using Enums;
using TMPro;
using System.Linq;
using Save;

namespace Menu.PopUps
{
    public class ArenaModInfoUI : MObject
    {
        #region Members

        ArenaModData m_ArenaModData;

        private TMP_Text    m_RewardDescription;
        private TMP_Text    m_Description;
        private Button      m_AddButton;
        private Button      m_RemoveButton;

        private Action m_Callback;

        #endregion


        #region Init

        protected override void FindComponents()
        {
            m_RewardDescription = Finder.FindComponent<TMP_Text>(gameObject,    "RewardDescription");
            m_Description       = Finder.FindComponent<TMP_Text>(gameObject,   "Description");
            m_AddButton         = Finder.FindComponent<Button>(gameObject,      "AddButton");
            m_RemoveButton      = Finder.FindComponent<Button>(gameObject,      "RemoveButton");
        }

        public void DisplayMod(EArenaMod arenaMod, Action callback)
        {
            m_Callback      = callback;
            m_ArenaModData  = AssetLoader.LoadArenaMod(arenaMod);

            m_RewardDescription.text    = m_ArenaModData.GetRewardDescription();
            m_Description.text          = m_ArenaModData.GetDescription();

            RefreshButtons();
        }

        void RefreshButtons()
        {
            // run in progress ? deactivate modifiers
            if (ProgressionCloudData.HasArenaInProgress)
            {
                m_AddButton.gameObject.SetActive(false);
                m_RemoveButton.gameObject.SetActive(false);
                return;
            }

            // check if should be added or removed
            if (PlayerPrefsHandler.CurrentArenaMods.Contains(m_ArenaModData.ArenaMod))
            {
                m_AddButton.gameObject.SetActive(false);
                m_RemoveButton.gameObject.SetActive(true);
            }
            else
            {
                m_AddButton.gameObject.SetActive(true);
                m_RemoveButton.gameObject.SetActive(false);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_AddButton.onClick.AddListener(OnButtonClicked);
            m_RemoveButton.onClick.AddListener(OnButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_AddButton.onClick.RemoveAllListeners();
            m_RemoveButton.onClick.RemoveAllListeners();
        }

        private void OnButtonClicked()
        {
            m_Callback?.Invoke();
            RefreshButtons();
        }

        #endregion

    }
}
