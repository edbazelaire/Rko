using Assets.Scripts.Managers;
using Enums;
using Menu.Common.Buttons;
using Menu.PopUps.Components.ProfilePopUp;
using Save;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Menu.MainMenu
{
    public class EmotsDisplayUI : MObject
    {
        #region Members

        GameObject          m_EmotsContainer;
        List<EmotButtonUI>  m_EmotButtons;

        int m_CurrentEmotIndex = 0;

        // ===============================================================================
        // Public Acessors
        public List<EmotButtonUI> EmotButtons => m_EmotButtons;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_EmotsContainer = Finder.Find(gameObject, "EmotsContainer");
        }

        public void Initialize(string[] emots, bool activateButtons = true)
        {
            base.Initialize();

            // setup Badges
            InitEmots(emots);

            // deactivate buttons by default
            SetButtonsActive(activateButtons);
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Set all buttons active or not
        /// </summary>
        /// <param name="active"></param>
        public void SetButtonsActive(bool active)
        {
            foreach (var emotButton in m_EmotButtons)
                emotButton.Button.interactable = active;
        }

        public void InitEmots(string[] emots)
        {
            if (emots == null)
            {
                ErrorHandler.Error("Null badges provided ");
                return;
            }

            if (emots.Length != ProfileCloudData.N_EMOTS_DISPLAYED)
            {
                ErrorHandler.Error("Number of badges " + emots.Length + " missmatch expected number of emots " + ProfileCloudData.N_EMOTS_DISPLAYED);
                return;
            }

            UIHelper.CleanContent(m_EmotsContainer);
            m_EmotButtons = new List<EmotButtonUI>();
            var templateEmot = AssetLoader.LoadAchievementRewardTemplate(EAchievementReward.Emot);
            for (int i = 0; i < emots.Length; i++)
            {
                if (i >= emots.Length)
                {
                    ErrorHandler.Warning("Not enough emots provided : " + emots.Length);
                    break;
                }

                m_EmotButtons.Add(Instantiate(templateEmot, m_EmotsContainer.transform).GetComponent<EmotButtonUI>());
                m_EmotButtons[i].Initialize(emots[i]);

                int index = i;
                m_EmotButtons[i].Button.onClick.AddListener(() => OnCurrentEmotButtonClicked(index));
            }
        }

        public void RefreshEmots(string[] emots)
        {
            for (int index = 0; index < m_EmotButtons.Count; index++)
            {
                m_EmotButtons[index].RefreshUI(emots[index]);
            }
        }

        /// <summary>
        /// Set badge at provided index as currently selected badge that can will be changed when selecting a new badge
        /// </summary>
        /// <param name="index"></param>
        void SelectEmot(int index)
        {
            DeselectCurrentEmot();

            if (index < 0 || index >= m_EmotButtons.Count)
            {
                ErrorHandler.Error("Emots list has no index " + index);
                return;
            }

            ProfileCloudData.LastSelectedEmotIndex = index;
            m_CurrentEmotIndex = index;
            m_EmotButtons[m_CurrentEmotIndex].SetSelected(true);
        }

        void DeselectCurrentEmot()
        {
            if (m_CurrentEmotIndex < 0)
                return;

            if (m_CurrentEmotIndex > m_EmotButtons.Count)
            {
                ErrorHandler.Error("Bades list has no index " + m_CurrentEmotIndex);
                return;
            }

            m_EmotButtons[m_CurrentEmotIndex].SetSelected(false);
            m_CurrentEmotIndex = -1;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            ProfileCloudData.CurrentDataChanged += OnCurrentDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProfileCloudData.CurrentDataChanged -= OnCurrentDataChanged;
        }

        void OnCurrentDataChanged(EAchievementReward achievementReward)
        {
            if (achievementReward != EAchievementReward.Emot)
                return;

            RefreshEmots(ProfileCloudData.CurrentEmots);
        }

        void OnCurrentEmotButtonClicked(int index)
        {
            // Re-click selected : close selection
            if (m_CurrentEmotIndex == index)
            {
                DeselectCurrentEmot();
            }

            // Click other than selected 
            if (m_CurrentEmotIndex != index)
            {
                // set clicked button as selected
                SelectEmot(index);
            }
        }

        #endregion

    }
}