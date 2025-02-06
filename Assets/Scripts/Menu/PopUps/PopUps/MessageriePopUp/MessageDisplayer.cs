using Assets;
using Assets.Scripts.Managers;
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

            // set that message has been seen
            NotificationCloudData.SetMessageSeen(message.Id);

            // activate / deactivate rewards and collect button if there is reward or not
            if (message.RewardsData.IsEmpty)
            {
                m_RewardsDisplayer.gameObject.SetActive(false);
                m_CollectButton.gameObject.SetActive(false);
            }
            else
            {
                m_RewardsDisplayer.gameObject.SetActive(true);
                m_RewardsDisplayer.Initialize(message.RewardsData);
                m_CollectButton.gameObject.SetActive(true);
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
            NotificationCloudData.DeleteMessage(m_Message.Id);
        }

        #endregion
    }
}