using Enums;
using Game.Loaders;
using Menu.Common.Notifications;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.Common.Buttons
{
    public class CharacterInfoButton : MObject
    {
        #region Members

        // Components & GameObject
        Button m_Button;
        Image m_Image;

        bool m_AllowsUpgrade;
        bool m_IsUpgradable = false;

        public Button Button => m_Button;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button        = Finder.FindComponent<Button>(gameObject);
            m_Image         = Finder.FindComponent<Image>(gameObject);
        }

        public void Initialize(ECharacter character, bool allowsUpgrade = true)
        {
            base.Initialize();

            m_AllowsUpgrade = allowsUpgrade;
            RefreshUI(character);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI(ECharacter character)
        {
            if (m_AllowsUpgrade && InventoryCloudData.Instance.GetCollectable(character).IsUpgradable())
                SetAsUpgradable();
            else
                SetAsInfo();

            RefreshMasteryParticles(character);
        }

        void SetAsInfo()
        {
            if (! m_IsUpgradable)
                return;

            m_IsUpgradable = false;
            m_Image.sprite = AssetLoader.Load<Sprite>("CharacterInfoButton", AssetLoader.c_ButtonsPath);
            NotificationParticles.Remove(gameObject);
        }

        void SetAsUpgradable()
        {
            if (m_IsUpgradable)
                return;

            m_IsUpgradable = true;
            m_Image.sprite = AssetLoader.Load<Sprite>("CharacterInfoButtonUpgradable", AssetLoader.c_ButtonsPath);
            NotificationParticles.Add(
                gameObject: gameObject, 
                background: m_Image, 
                size: Vector2.one,
                colorHexa: null
            );
        }

        /// <summary>
        /// 
        /// </summary>
        void RefreshMasteryParticles(ECharacter character)
        {
            int nAchivementsToCollect = 0;
            foreach (var achievementData in AchievementLoader.Achievements)
            {
                if (achievementData.IsUnlockable && achievementData.IsCharacterMastery && achievementData.Character == character)
                    nAchivementsToCollect++;
            }

            if (nAchivementsToCollect > 0)
            {
                NotificationPulse.Add(
                    baseGameObject:     gameObject,
                    animationTarget:    gameObject,
                    redDotTarget:       gameObject,
                    counter:            nAchivementsToCollect
                );
            } else
            {
                NotificationPulse.Remove(gameObject);
            }
        }

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
