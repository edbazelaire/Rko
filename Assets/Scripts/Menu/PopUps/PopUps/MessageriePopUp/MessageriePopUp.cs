using Save;
using Tools;
using UnityEngine;

namespace Menu.PopUps.Messagerie
{
    public class MessageriePopUp : PopUp
    {
        #region Members

        GameObject m_MessagesContainer;
        MessageDisplayer m_MessageDisplayer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_MessagesContainer = Finder.Find(gameObject, "MessagesContainer");
            m_MessageDisplayer = Finder.FindComponent<MessageDisplayer>(gameObject);
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();

            m_MessageDisplayer.Initialize();

            RefreshMessages();
        }

        #endregion


        #region GUI Manipulators

        void RefreshMessages()
        {
            UIHelper.CleanContent(m_MessagesContainer);

            if (NotificationCloudData.Messages.Count == 0)
            {
                SetupNoMessageUI();
                return;
            }

            MessageItem messageItem = AssetLoader.Load<MessageItem>(AssetLoader.c_PopUpsPath);
            foreach (var message in NotificationCloudData.Messages)
            {
                var tempMessageItem = Instantiate(messageItem, m_MessagesContainer.transform);
                tempMessageItem.Initialize(message);
                tempMessageItem.Button.onClick.AddListener(() => OnMessageClicked(message));
            }

            m_MessageDisplayer.Display(NotificationCloudData.Messages[0]);
        }

        void SetupNoMessageUI()
        {
            m_MessageDisplayer.DisplayNoMessage();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            NotificationCloudData.MessageCountChangedEvent += RefreshMessages;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            NotificationCloudData.MessageCountChangedEvent -= RefreshMessages;
        }

        void OnMessageClicked(SMessage message)
        {
            m_MessageDisplayer.Display(message);
        }

        #endregion
    }
}