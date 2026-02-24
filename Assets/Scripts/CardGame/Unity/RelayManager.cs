using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Gère la connexion P2P via Unity Relay : créer une partie (Host, code ami) ou rejoindre avec un code.
    /// Nécessite un NetworkManager dans la scène avec UnityTransport en mode Relay.
    /// </summary>
    public class RelayManager : MonoBehaviour
    {
        public const string ConnectionType = "dtls";
        public const int MaxConnections = 2;

        private bool _servicesInitialized;

        /// <summary>True une fois Unity Services et Auth initialisés.</summary>
        public bool IsReady => _servicesInitialized;

        /// <summary>Initialise Unity Services et l'authentification anonyme. À appeler avant CreateOrJoin.</summary>
        public async Task InitializeAsync()
        {
            if (_servicesInitialized) return;
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                _servicesInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayManager] Init failed: {e.Message}");
                throw;
            }
        }

        /// <summary>Crée une allocation Relay et démarre le Host. Retourne le code ami à afficher.</summary>
        public async Task<string> StartHostWithRelayAsync()
        {
            await InitializeAsync();
            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[RelayManager] UnityTransport not found on NetworkManager.");
                return null;
            }
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (NetworkManager.Singleton.StartHost())
                return joinCode;
            Debug.LogError("[RelayManager] StartHost failed.");
            return null;
        }

        /// <summary>Rejoint une partie avec le code ami et démarre le Client.</summary>
        public async Task<bool> StartClientWithRelayAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogWarning("[RelayManager] Join code is empty.");
                return false;
            }
            joinCode = joinCode.Trim();
            await InitializeAsync();
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[RelayManager] UnityTransport not found on NetworkManager.");
                return false;
            }
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, ConnectionType));
            return NetworkManager.Singleton.StartClient();
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
    }
}
