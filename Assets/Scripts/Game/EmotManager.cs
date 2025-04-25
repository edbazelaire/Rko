using Enums;
using Game.UI;
using Game;
using System;
using System.Collections;
using Tools;
using Unity.Netcode;
using UnityEngine;
using Menu.Common.Dots;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace Assets.Scripts.Game
{
    public class EmotManager : NetworkBehaviour
    {
        #region Members

        public static EmotManager Instance;

        // [CLIENT] save currently displayed emots to cancel them when playing a new emot
        Dictionary<ulong, EmotUI> m_CurrentEmots = new Dictionary<ulong, EmotUI>();

        #endregion


        #region Inti & End

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #endregion


        #region Emot Display

        public void RequestDisplayEmot(EEmot emot, ulong clientId)
        {
            DisplayEmot(emot, clientId);

            // Server sends display request to target client
            if (IsServer)
            {
                DisplayEmotClientRpc(emot, clientId);
            } else
            {
                DisplayEmotServerRpc(emot, clientId);
            }
        }

        void DisplayEmot(EEmot emot, ulong clientId)
        {
            var player = GameManager.Instance.GetPlayer(clientId);
            if (player == null)
            {
                ErrorHandler.Error("Unable to find player : " + clientId);
                return;
            }

            // check if already has an emot active
            if (m_CurrentEmots.ContainsKey(clientId) && !m_CurrentEmots[clientId].IsDestroyed())
                m_CurrentEmots[clientId].End();

            var emotUI = AssetLoader.Load<EmotUI>("Emot", AssetLoader.c_EmotsPath);
            m_CurrentEmots[clientId] = Instantiate(emotUI, player.transform);
            m_CurrentEmots[clientId].Initialize(emot);
            m_CurrentEmots[clientId].transform.localPosition += new Vector3(0f, 1.2f, 0f);
            m_CurrentEmots[clientId].transform.localScale *= 0.2f;
        }

        [ServerRpc]
        void DisplayEmotServerRpc(EEmot emot, ulong clientId)
        {
            // display on server side (for Host)
            DisplayEmot(emot, clientId);

            // call client to display the emot
            DisplayEmotClientRpc(emot, clientId);
        }

        [ClientRpc]
        void DisplayEmotClientRpc(EEmot emot, ulong clientId)
        {
            // already displayed when clicked so no need to re-display it
            if (clientId == GameManager.Instance.Owner.PlayerId)
                return;

            DisplayEmot(emot, clientId);
        }

        #endregion
    }
}