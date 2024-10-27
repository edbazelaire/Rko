using Save;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.PopUps.Messagerie
{
    public class MessageItem : MObject
    {
        #region Members

        SMessage m_Message;

        TMP_Text m_Title;
        Image m_SeenImage;
        Button m_Button;

        public Button Button => m_Button;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Title = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_SeenImage = Finder.FindComponent<Image>(gameObject, "Seen");
            m_Button = Finder.FindComponent<Button>(gameObject);
        }

        public void Initialize(SMessage message)
        {
            m_Message = message;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Title.text = m_Message.Title;
            SetSeen(m_Message.Seen);
        }

        #endregion


        #region GUI Manipulators

        void SetSeen(bool isSeen)
        {
            m_SeenImage.gameObject.SetActive(!isSeen);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            NotificationCloudData.MessageSeenEvent += OnMessageSeen;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            NotificationCloudData.MessageSeenEvent -= OnMessageSeen;
        }

        void OnMessageSeen(string id)
        {
            if (m_Message.Id != id)
                return;

            SetSeen(true);
        }

        #endregion
    }
}
