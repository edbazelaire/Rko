using System.Linq;
using Enums;
using Game.UI;
using Menu.Common.Buttons;
using Save;
using Tools;
using Unity.Services.Lobbies.Models;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps.Components.ProfilePopUp
{
    public class EmotButtonUI : AchievementRewardUI
    {
        #region Members

        // =================================================================================
        // GameObjects & Components
        GameObject m_EmotContainer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
            m_EmotContainer = Finder.Find(gameObject, "EmotContainer");
        }

        public override void Initialize(string emotName, EAchievementReward ar = EAchievementReward.Emot)
        {
            base.Initialize(emotName, ar);
            RefreshUI(emotName);
        }


        #endregion


        #region GUI Manipulators

        public void RefreshUI(string emotName)
        {
            m_Name = emotName;

            UIHelper.CleanContent(m_EmotContainer);
            var emotUI = Instantiate(AssetLoader.Load<EmotUI>("Emot", AssetLoader.c_EmotsPath), m_EmotContainer.transform);
            emotUI.Initialize(emotName, startTimer: false);
        }

        #endregion


        #region Button Methods

        public override void SetAsCurrent()
        {
            if (ProfileCloudData.CurrentEmots.Contains(m_Name))
            {
                // TODO : error message ?
                ErrorHandler.Log("Emot " + m_Name + " already used");
                return;
            }

            var index = ProfileCloudData.LastSelectedEmotIndex;
            ProfileCloudData.SetCurrentEmot(m_Name, index);
        }

        #endregion
    }
}