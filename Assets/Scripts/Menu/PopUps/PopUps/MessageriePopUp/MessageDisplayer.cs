using Assets;
using Menu.Common.Displayers;
using Save;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps.Messagerie
{
    public class MessageDisplayer : MObject
    {
        #region Members

        SMessage m_Message;

        TMP_Text            m_MessageTitle;
        RewardsDisplayer    m_RewardsDisplayer;
        TMP_Text            m_MessageContentText;
        GameObject          m_ButtonsContainer;
        Button              m_CollectButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_MessageTitle          = Finder.FindComponent<TMP_Text>(gameObject, "MessageTitle");
            m_RewardsDisplayer      = Finder.FindComponent<RewardsDisplayer>(gameObject, "RewardsDisplayer");
            m_MessageContentText    = Finder.FindComponent<TMP_Text>(gameObject, "MessageContentText");
            m_ButtonsContainer      = Finder.Find(gameObject, "MessageButtonsContainer");
            m_CollectButton         = Finder.FindComponent<Button>(m_ButtonsContainer, "CollectButton");
        }


        #endregion


        #region Display Message

        public void Display(SMessage message)
        {
            m_Message = message;
            m_MessageTitle.text = message.Title;
            m_MessageContentText.text = message.Content;

            if (message.RewardsData.IsEmpty)
            {
                m_RewardsDisplayer.gameObject.SetActive(false);
                m_CollectButton.gameObject.SetActive(false);
                NotificationCloudData.SetMessageSeen(message.Id);
            }
            else
            {
                m_RewardsDisplayer.gameObject.SetActive(true);
                m_RewardsDisplayer.Initialize(message.RewardsData);
                m_CollectButton.gameObject.SetActive(true);

                m_CollectButton.interactable = !m_Message.Seen;
            }
        }

        public void DisplayNoMessage()
        {
            m_MessageTitle.text = "";
            m_MessageContentText.text = "No new messages";
            m_CollectButton.gameObject.SetActive(false); 
            m_RewardsDisplayer.gameObject.SetActive(false);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_CollectButton.onClick.AddListener(OnCollectButtonClicked);
        }

        void OnCollectButtonClicked()
        {
            Main.DisplayRewards(m_Message.RewardsData, "Messagerie", OnRewardCollected);
        }

        void OnRewardCollected()
        {
            NotificationCloudData.SetMessageSeen(m_Message.Id);
            m_CollectButton.interactable = false;
        }

        #endregion
    }
}