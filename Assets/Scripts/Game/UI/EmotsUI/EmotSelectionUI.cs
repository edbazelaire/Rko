using Assets.Scripts.Game;
using Enums;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Game.UI
{
    public class EmotSelectionUI : MObject
    {
        #region Members

        Button m_Button;
        Image m_Icon;

        EEmot m_Emot;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button = Finder.FindComponent<Button>(gameObject);
            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
        }

        public virtual void Initialize(EEmot emot)
        {
            m_Emot = emot;
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Icon.sprite = AssetLoader.Load<Sprite>(m_Emot.ToString(), AssetLoader.c_EmotsSpritePath);
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClick);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveAllListeners();
        }

        void OnClick()
        {
            EmotManager.Instance.RequestDisplayEmot(m_Emot, GameManager.Instance.Owner.PlayerId);
        }

        #endregion
    }
}
