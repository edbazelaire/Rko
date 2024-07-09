using Assets;
using Data.GameManagement;
using Enums;
using Network;
using System.Collections;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class LobbyScreen : OverlayScreen
    {
        #region Members

        [SerializeField] string m_NumPlayersMessage = "Players found {0} / {1}";

        TMP_Text m_MessageText;
        TMP_Text m_SubMessageText;
        Image m_Logo;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_MessageText = Finder.FindComponent<TMP_Text>(gameObject, "MessageText");
            m_SubMessageText = Finder.FindComponent<TMP_Text>(gameObject, "SubMessageText");
            m_Logo = Finder.FindComponent<Image>(gameObject, "Logo");
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();

            SetupMessage();

            if (LobbyHandler.Instance.GameMode != EGameMode.Ranked)
                m_SubMessageText.gameObject.SetActive(false);
            else
                Settings.Reload();
            

            ApplyLogoAnimation();
        }

        #endregion


        #region GUI Manipulators

        void SetupMessage()
        {
            switch (LobbyHandler.Instance.GameMode)
            {
                case EGameMode.Ranked:
                    m_MessageText.text = "Looking for Players";
                    return;

                case EGameMode.Arena:
                case EGameMode.Training:
                    m_MessageText.text = "Preparing Game...";
                    return;

                default:
                    ErrorHandler.Warning("Unhandled case " + LobbyHandler.Instance.GameMode);
                    m_MessageText.text = "";
                    return;
            }
        }

        #endregion


        #region Inherited Manipulators

        protected override void Update()
        {
            base.Update();

            if (! m_Initialized)
                return;

            RefreshSubMessage();
        }

        #endregion


        #region GUI Manipulators

        void RefreshSubMessage()
        {
            switch (LobbyHandler.Instance.GameMode)
            {
                case EGameMode.Ranked:
                    m_SubMessageText.text = string.Format(m_NumPlayersMessage, LobbyHandler.Instance.NPlayers, LobbyHandler.Instance.MaxPlayers);
                    break;

                default:
                    return;
            }
        }

        void ApplyLogoAnimation()
        {
            var rotateAnim = m_Logo.gameObject.AddComponent<RotateAnimation>();
            rotateAnim.Initialize(duration: -1, rotation: new Vector3(0, 0, -360));
        }

        #endregion


        #region Listeners

        protected override void OnCancelButton()
        {
            LobbyHandler.Instance.LeaveLobby();
            Main.SetState(EAppState.MainMenu);

            base.OnCancelButton();
        }

        #endregion
    }
}