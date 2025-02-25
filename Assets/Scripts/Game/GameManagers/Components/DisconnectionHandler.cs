using Game.UI;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.GameManagers.Components
{
    public enum EErrorType
    {
        None,

        ClientDisconnected,
        HostDisconnected,
        ServerDown,
    }

    public class DisconnectionHandler : MonoBehaviour
    {
        static DisconnectionHandler s_Instance;
        public static DisconnectionHandler Instance => s_Instance;

        [SerializeField] float      m_ReconnectionTimeout   = 5f; // Timeout period in seconds
        [SerializeField] string     m_ServerDownMessage     = "The server went down for unexpected reasons"; 
        [SerializeField] string     m_ReconnectionMessage   = "Your opponent has been disconnected"; 
        [SerializeField] string     m_CountdownMessage      = "The game will end in... {0}";

        List<EErrorType>    m_DisconnectionReasons      = new List<EErrorType>();
        List<ulong>         m_DisconnectedClients       = new List<ulong>();
        float               m_DisconnectionTimestamp    = 0f;

        bool m_IsPaused = false;
        bool m_IsWaitingServer              => m_DisconnectionReasons.Contains(EErrorType.ServerDown);
        bool m_IsWaitingForReconnection     => m_DisconnectedClients.Count > 0;
        bool m_IsHostDisconnected           => m_DisconnectedClients.Contains(NetworkManager.ServerClientId);


        #region Init & End

        public void Start()
        {
            s_Instance = this;

            m_DisconnectionReasons = new();
            m_DisconnectedClients = new();

            NetworkManager.Singleton.OnServerStopped            += OnServerStopped;
            NetworkManager.Singleton.OnServerStarted            += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public void OnDestroy()
        {
            s_Instance = null;

            if (NetworkManager.Singleton == null) 
                return;

            NetworkManager.Singleton.OnServerStopped            -= OnServerStopped;
            NetworkManager.Singleton.OnServerStarted            -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        public static void End()
        {
            if (s_Instance == null)
                return;

            Destroy(s_Instance.gameObject);
        }

        #endregion


        #region Handling Deconnection

        private void PauseGame(EErrorType errorType)
        {
            if (m_IsPaused)
            {
                ErrorHandler.Warning("Call Pause() on the game while already in Pause");
                return;
            }

            switch (errorType)
            {
                case EErrorType.HostDisconnected:
                case EErrorType.ClientDisconnected:
                    NotifyPlayers(m_ReconnectionMessage);
                    StartCoroutine(WaitForClientReconnection());
                    break;

                // ========================================================================
                // OLD METHOD (remove ?) not working because GameObject is destroyed when ServerDown
                //case EErrorType.ServerDown:
                //    NotifyPlayers(m_ServerDownMessage);
                //    StartCoroutine(WaitServerRestart());
                //    break;
                // ========================================================================

                case EErrorType.ServerDown:
                    GameManager.ExitWithError(m_ServerDownMessage, refund: ! GameManager.Instance.IsServer);
                    return;

                default:
                    ErrorHandler.Error("Unhandled case : " + errorType);
                    return;
            }

            // stop time scale
            Time.timeScale = 0f;
            m_IsPaused = true;
        }

        private void ResumeGame()
        {
            ErrorGameUI.Hide();

            // Implement game resuming logic here
            Time.timeScale = 1f;
            m_IsPaused = false;
        }

        private void NotifyPlayers(string message)
        {
            // Implement player notification logic here
            Debug.LogWarning(message);

            ErrorGameUI.Display(message);
        }

        private IEnumerator WaitForClientReconnection()
        {
            // =====================================================================================
            // Not working because GameManager is destroyed when the server goes down
            float timePassed = Time.unscaledTime - m_DisconnectionTimestamp;
            while (timePassed < m_ReconnectionTimeout)
            {
                timePassed = Time.unscaledTime - m_DisconnectionTimestamp;
                if (! m_IsWaitingForReconnection)
                {
                    // Host has reconnected
                    ResumeGame();
                    yield break;
                }

                ErrorGameUI.SetSubMessage(string.Format(m_CountdownMessage, Mathf.Ceil(m_ReconnectionTimeout - timePassed)));
                yield return null;
            }
            // =====================================================================================

            ErrorGameUI.Hide();

            // end the game
            EndGame();

            // resume to the game
            Time.timeScale = 1f;
        }

        private IEnumerator WaitServerRestart()
        {
            float timePassed;
            do
            {
                if (!m_IsWaitingServer)
                {
                    // Server is back
                    ResumeGame();
                    yield break;
                }

                timePassed = Time.unscaledTime - m_DisconnectionTimestamp;
                ErrorGameUI.SetSubMessage(string.Format(m_CountdownMessage, Mathf.Ceil(m_ReconnectionTimeout - timePassed)));
                yield return null;
            } while (timePassed < m_ReconnectionTimeout);

            ErrorGameUI.Hide();

            // end the game
            EndGame();

            // resume to the game
            Time.timeScale = 1f;
        }

        private void EndGame()
        {
            // force deactivation of the Intro 
            if (GameUIManager.IntroGameUI != null && GameUIManager.IntroGameUI.isActiveAndEnabled) 
            {
                GameUIManager.IntroGameUI.Deactivate();
            }

            // no host or server - insta display end of game
            if (m_IsHostDisconnected || m_IsWaitingServer)
            {
                GameUIManager.Instance.SetUpGameOver(true);
                return;
            }

            // host still alive - shutdown game then display end of game
            int winningTeam;

            // both players are disconnected : no winner
            if (m_DisconnectedClients.Count == 0 && m_DisconnectionReasons.Contains(EErrorType.ClientDisconnected))
            {
                ErrorHandler.Error("EndGame() called by the DeconnectionHandler but no disconnected player were found");
                return;
            }
            else if (m_DisconnectedClients.Count >= 2)
                winningTeam = -1;
            else
                winningTeam = m_DisconnectedClients[0] != GameManager.Instance.Owner.PlayerId ? GameManager.Instance.Owner.Team : (GameManager.Instance.Owner.Team + 1) % 2;

            GameManager.Instance.GameOverClientRPC(winningTeam);
        }

        #endregion


        #region Listeners

        void OnServerStarted()
        {
            if (! m_IsWaitingServer)
                return;

            m_DisconnectionReasons.Remove(EErrorType.ServerDown);
        }

        void OnServerStopped(bool stopped)
        {
            if (m_IsWaitingServer == stopped)
                return;

            m_DisconnectionTimestamp = Time.unscaledTime;

            if (stopped)
            {
                m_DisconnectionReasons.Add(EErrorType.ServerDown);
                PauseGame(EErrorType.ServerDown);
            }
            else
            {
                m_DisconnectionReasons.Remove(EErrorType.ServerDown);
                ResumeGame();
            }
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        /// <param name="clientId"></param>
        void OnClientDisconnected(ulong clientId)
        {
            if (GameManager.IsGameOver)
                return;

            EErrorType errorType = EErrorType.ClientDisconnected;
            bool wasWaiting = m_IsWaitingForReconnection;

            // add client to list of disconnected client
            if (m_DisconnectedClients.Contains(clientId))
                ErrorHandler.Error("Client " + clientId + " already in list of disconected clients");
            else
                m_DisconnectedClients.Add(clientId);

            m_DisconnectionTimestamp = Time.unscaledTime;
            
            if (NetworkManager.ServerClientId == clientId)
            {
                // HOST DISCONNECTED
                Debug.Log("HOST HAS BEEN DISCONNECTED");
                errorType = EErrorType.HostDisconnected;
            }

            if (! wasWaiting)
            {
                PauseGame(errorType);
            }
        }

        /// <summary>
        /// Called when a client re-connects
        /// </summary>
        /// <param name="clientId"></param>
        void OnClientConnected(ulong clientId) 
        {
            if (GameManager.IsGameOver || !GameManager.Instance.IsGameLoaded)
                return;

            if (! m_DisconnectedClients.Contains(clientId))
                ErrorHandler.Error("Client " + clientId + " not found in list of disconected clients");
            else
                m_DisconnectedClients.Remove(clientId);
        }

        #endregion
    }
}