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
        private string _lastError;

        /// <summary>True une fois Unity Services et Auth initialisés.</summary>
        public bool IsReady => _servicesInitialized;
        /// <summary>Dernier message d'erreur (format code invalide, connexion échouée, etc.).</summary>
        public string LastError => _lastError ?? "";

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

        /// <summary>Caractères autorisés pour le code Relay (6-12 caractères).</summary>
        private static readonly System.Text.RegularExpressions.Regex JoinCodeRegex = new System.Text.RegularExpressions.Regex("^[6789BCDFGHJKLMNPQRTWbcdfghjklmnpqrtw]{6,12}$");

        /// <summary>Rejoint une partie avec le code ami et démarre le Client.</summary>
        public async Task<bool> StartClientWithRelayAsync(string joinCode)
        {
            _lastError = null;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                _lastError = "Code vide.";
                return false;
            }
            joinCode = joinCode.Trim();
            if (!JoinCodeRegex.IsMatch(joinCode))
            {
                _lastError = joinCode.Length < 6
                    ? $"Code trop court : {joinCode.Length} caractères (minimum 6). Vérifie que tu as bien copié tout le code."
                    : $"Code invalide : 6 à 12 caractères (chiffres 6-9, lettres sans A,E,I,O,S,V,X,Y,Z).";
                return false;
            }
            try
            {
                await InitializeAsync();
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    _lastError = "Erreur configuration réseau.";
                    return false;
                }
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, ConnectionType));
                var ok = NetworkManager.Singleton.StartClient();
                if (!ok) _lastError = "Connexion échouée.";
                return ok;
            }
            catch (Exception e)
            {
                _lastError = e.Message.Contains("400") || e.Message.Contains("Bad Request")
                    ? "Code invalide ou expiré. Demande un nouveau code au créateur de la partie."
                    : e.Message;
                return false;
            }
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
    }
}
