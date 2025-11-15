using Enums;
using Game.UI;
using Game;
using Tools;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI;
using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using static UnityEngine.GraphicsBuffer;

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

            if (GameManager.Instance.IsOfflineMode)
                return;

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

            // check si déjà un emot actif
            if (m_CurrentEmots.ContainsKey(clientId) && !m_CurrentEmots[clientId].IsDestroyed())
                m_CurrentEmots[clientId].End();

            // charge prefab UI
            var emotUI = AssetLoader.Load<EmotUI>("Emot", AssetLoader.c_EmotsPath);

            // instancie dans le HUD canvas
            m_CurrentEmots[clientId] = Instantiate(emotUI, GameUIManager.Instance.Canvas.transform);

            // Position en pixels écran
            Vector3 screenPos = Camera.main.WorldToScreenPoint(player.transform.position + new Vector3(0.5f, 0.7f, 0));

            // Conversion écran -> local UI
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GameUIManager.Instance.Canvas.transform as RectTransform,
                screenPos,
                GameUIManager.Instance.Canvas.worldCamera,  // la caméra du canvas
                out Vector2 localPos
            );

            // Applique la position locale
            m_CurrentEmots[clientId].GetComponent<RectTransform>().localPosition = localPos;

            // init game object
            m_CurrentEmots[clientId].Initialize(emot, startTimer: true);
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