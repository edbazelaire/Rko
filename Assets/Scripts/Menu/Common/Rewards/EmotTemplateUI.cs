using Enums;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.Common.Rewards
{
    public class EmotTemplateUI : MObject
    {
        #region Members

        TMP_Text        m_Title;
        Image           m_Icon;
        GameObject      m_Aura;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Title = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Aura = Finder.Find(gameObject, "Aura");
        }

        public virtual void Initialize(EEmot emot)
        {
            base.Initialize();

            m_Title.text = emot.ToString();
            m_Icon.sprite = AssetLoader.Load<Sprite>(emot.ToString(), AssetLoader.c_EmotsSpritePath);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

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