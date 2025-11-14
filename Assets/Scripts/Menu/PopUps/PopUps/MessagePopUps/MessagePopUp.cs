using System;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class MessagePopUp : PopUp
    {
        #region Members

        // data
        protected string m_TitleData;
        protected string m_Message;

        // GameObjects & Components
        protected GameObject m_MessageContainer;
        protected TMP_Text m_MessageText;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            var content = Finder.Find(gameObject, "Content");
            m_MessageContainer = Finder.Find(content, "MessageContainer", false);
            m_MessageText = Finder.FindComponent<TMP_Text>(m_MessageContainer != null ? m_MessageContainer : gameObject, "Message");
        }

        public void Initialize(string message = "", string title = "", Action onValidate = null, Action onCancel = null)
        {
            base.Initialize(onValidate, onCancel);

            m_Message = message;
            m_TitleData = title;
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            SetUpTitle();
            SetUpMessage();
            
        }

        #endregion


        #region GUI Manipulators

        protected virtual void SetUpTitle()
        {
            if (m_Title == null)
                return;
            
            if (m_TitleData == null || m_TitleData == "")
            {
                m_Title.gameObject.SetActive(false);
            }

            m_Title.text = m_TitleData;
        }


        protected virtual void SetUpMessage()
        {
            if (m_Message == null || m_Message == "")
            {
                if (m_MessageContainer != null)
                    m_MessageContainer.SetActive(false);
                else if (m_Message != null)
                    m_MessageText.gameObject.SetActive(false);

                return;
            }

            if (m_MessageText == null)
                return;

            m_MessageText.text = m_Message;
        }

        #endregion
    }
}