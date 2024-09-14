using Enums;
using MyBox;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tools;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Scheduler;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Assets.Scripts.Network
{
    public class RelayHandler : MonoBehaviour
    {
        #region Members

        // ===================================================================================
        // STATIC
        public const int MAX_RETRY_INITIALIZATION = 1;
        
        // ===================================================================================
        // PRIVATE VARIABLES 
        static      RelayHandler s_Instance     = null;
        bool        m_Initialized               = false;

        // ===================================================================================
        // PUBLIC ACCESSORS
        public static RelayHandler Instance     => s_Instance;
        public static bool Initialized          => Instance != null && Instance.m_Initialized;

        #endregion


        #region Init & End

        void Start()
        {
            // Initialize Instance
            s_Instance = this;

            // keep it in all scenes
            DontDestroyOnLoad(gameObject);

            // tell the game that it has been initialized properly
            m_Initialized = true;
        }

        #endregion


        #region Relay

        public async Task<string> CreateRelay()
        {
            ErrorHandler.Log("RelayHandler.CreateRelay()", ELogTag.System);

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                ErrorHandler.Log("Created relay with code " + joinCode, ELogTag.System);

                RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                NetworkManager.Singleton.StartHost();

                return joinCode;

            }
            catch (RelayServiceException e)
            {
                ErrorHandler.Error(e.Message);
            }

            return "";
        }

        public async Task JoinRelay(string joinCode)
        {
            ErrorHandler.Log("Joining relay with code " + joinCode, ELogTag.System);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

            ErrorHandler.Log("Client started with local id : " + NetworkManager.Singleton.LocalClientId, ELogTag.System);
        }

        #endregion


        #region Region Management

        public async Task<string> FindRegion()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
                return allocation.Region;
            }
            catch (RelayServiceException e)
            {
                ErrorHandler.Error(e.Message);
            }

            return "";
        }

        public static string TrimRegion(string regionName)
        {
            Regex regex = new Regex(@"(\D+)\d*$");
            Match match = regex.Match(regionName);
            return match.Groups[1].Value;
        }

        #endregion
    }
}