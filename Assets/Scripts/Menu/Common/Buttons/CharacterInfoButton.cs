using Enums;
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

        public Button Button => m_Button;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button        = Finder.FindComponent<Button>(gameObject);
            m_Image         = Finder.FindComponent<Image>(gameObject);
        }

        public void Initialize(ECharacter character)
        {
            base.Initialize();

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
            if (InventoryCloudData.Instance.GetCollectable(character).IsUpgradable())
                SetAsUpgradable();
            else
                SetAsInfo();
        }

        void SetAsInfo()
        {
            m_Image.sprite = AssetLoader.Load<Sprite>("CharacterInfoButton", AssetLoader.c_ButtonsPath);
            NotificationDisplay.Remove(gameObject);
        }

        void SetAsUpgradable()
        {
            m_Image.sprite = AssetLoader.Load<Sprite>("CharacterInfoButtonUpgradable", AssetLoader.c_ButtonsPath);
            NotificationDisplay.Add(
                gameObject: gameObject, 
                background: m_Image, 
                size: Vector2.one,
                colorHexa: null
            );
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
