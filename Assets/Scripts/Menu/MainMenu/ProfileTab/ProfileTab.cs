using Game.Loaders;
using Menu.Common.Notifications;
using Save;
using System.Collections;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu.ProfileTab
{
    public class ProfileTab : MainMenuTabContent
    {
        #region Members

        AchievementsTabManager  m_AchievementsTabManager;
        ProfileDisplayBE        m_ProfileDisplayBE;
        EmotsDisplayUI          m_EmotsDisplay;
        Button                  m_SwitchButton;

        int m_NAchivementsToCollect;
        Coroutine m_SwitchCoroutine;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_AchievementsTabManager    = Finder.FindComponent<AchievementsTabManager>(gameObject);
            m_ProfileDisplayBE          = Finder.FindComponent<ProfileDisplayBE>(gameObject, "ProfileDisplay");
            m_EmotsDisplay              = Finder.FindComponent<EmotsDisplayUI>(gameObject, "EmotsDisplay");
            m_SwitchButton              = Finder.FindComponent<Button>(gameObject, "SwitchButton");
        }

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            m_SwitchCoroutine = null;

            RefreshTabButtonNotifications();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_ProfileDisplayBE.Initialize();
            m_EmotsDisplay.Initialize(ProfileCloudData.CurrentEmots);

            m_ProfileDisplayBE.gameObject.SetActive(true);
            m_EmotsDisplay.gameObject.SetActive(false);
        }

        #endregion


        #region GUI Manipulators

        void ToggleDisplay()
        {
            if (m_SwitchCoroutine != null)
                return;

            SwitchToSide(profileSide: !m_ProfileDisplayBE.gameObject.activeInHierarchy);
        }

        void SwitchToSide(bool profileSide)
        {
            // play switch animation
            m_SwitchCoroutine = StartCoroutine(StartSwitchAnimation(
                toDeactivate: profileSide ? m_EmotsDisplay.gameObject : m_ProfileDisplayBE.gameObject,
                toActivate: profileSide ? m_ProfileDisplayBE.gameObject : m_EmotsDisplay.gameObject
            ));
        }

        IEnumerator StartSwitchAnimation(GameObject toDeactivate, GameObject toActivate)
        {
            // play first rotation
            RotateAnimation rotate = toDeactivate.AddComponent<RotateAnimation>();
            rotate.Initialize(duration: 0.25f, rotation: new Vector3(0f, 90f, 0f));
            yield return new WaitUntil(() => rotate.IsOver);

            // deactivate / activate objects
            toActivate.transform.rotation = Quaternion.Euler(new Vector3(0f, -90f, 0f));
            toDeactivate.SetActive(false);
            toActivate.SetActive(true);

            // finish rotation
            rotate = toActivate.AddComponent<RotateAnimation>();
            rotate.Initialize(duration: 0.25f, rotation: new Vector3(0f, 0f, 0f));
            yield return new WaitUntil(() => rotate.IsOver);

            m_SwitchCoroutine = null;
        }

        void EndCoroutine()
        {
            StopCoroutine(m_SwitchCoroutine);
            m_SwitchCoroutine = null;
            m_ProfileDisplayBE.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
            m_EmotsDisplay.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        }

        #endregion


        #region Notifications

        void RefreshTabButtonNotifications()
        {
            m_NAchivementsToCollect = 0;
            foreach (var achievementData in AchievementLoader.Achievements)
            {
                if (achievementData.IsUnlockable)
                    m_NAchivementsToCollect++;
            }

            if (m_NAchivementsToCollect < 0)
            {
                ErrorHandler.Error("m_NAchivementsToCollect (" + m_NAchivementsToCollect + ") < 0");
                m_NAchivementsToCollect = 0;
            }

            if (m_NAchivementsToCollect > 0)
            {
                NotificationParticles.Add(m_TabButton.gameObject, m_TabButton.BackgroundImage, new Vector2(1f, 1f));
            }
            else
            {
                NotificationParticles.Remove(m_TabButton.gameObject);
            }
        }

        #endregion



        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            m_AchievementsTabManager.TabSelectedEvent   += OnTabSelected;
            ProfileCloudData.AchievementChangedEvent    += OnAchievementChanged;
            ProfileCloudData.AchievementCompletedEvent  += OnAchievementChanged;
            m_SwitchButton.onClick.AddListener(ToggleDisplay);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            ProfileCloudData.AchievementChangedEvent    -= OnAchievementChanged;
            ProfileCloudData.AchievementCompletedEvent  -= OnAchievementChanged;
            m_SwitchButton.onClick.RemoveAllListeners();
        }

        protected void OnTabSelected(string tabName)
        {
            if (tabName == EAchievementTab.Emots.ToString() && m_ProfileDisplayBE.gameObject.activeInHierarchy)
            {
                if (m_SwitchCoroutine != null)
                    EndCoroutine();
                SwitchToSide(false);
            }

            else if (tabName != EAchievementTab.Emots.ToString() && ! m_ProfileDisplayBE.gameObject.activeInHierarchy)
            {
                if (m_SwitchCoroutine != null)
                    EndCoroutine();
                SwitchToSide(true);
            }

        }

        protected void OnAchievementChanged(string _)
        {
            RefreshTabButtonNotifications();
        }

        #endregion
    }
}