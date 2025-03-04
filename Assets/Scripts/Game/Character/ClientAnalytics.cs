using Analytics.Events;
using Enums;
using Managers;
using Network;
using Tools;
using Unity.Netcode;

namespace Game.Character
{
    public class ClientAnalytics : NetworkBehaviour
    {
        #region Members

        Controller m_Controller;

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
        }

        #endregion


        #region Analytics Manipulators

        [ClientRpc]
        public void SendSpellDataClientRPC(string spell, EHitType hitType, int qty)
        {
            // send event to analytics
            SendSpellData(spell, hitType, qty);
        }

        public void SendSpellData(string spell, EHitType hitType, int qty)
        {
            if (! CanSendData)
                return;

            // send event to analytics
            MAnalytics.SendEvent(new InGameEvent(LobbyHandler.Instance.GameMode, LobbyHandler.Instance.RelayCode, StaticPlayerData.Character, spell, hitType.ToString(), qty));
        }

        #endregion


        #region Helpers

        bool CanSendData
        {
            get
            {
                return IsOwner
                    && IsClient
                    && m_Controller.IsPlayer;
            }
        }

        #endregion

    }
}