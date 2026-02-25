using System;
using CardGame.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardGame.Unity
{
    /// <summary>
    /// État réseau du Lobby : choix de deck Host (Joueur 1) et Client (Joueur 2).
    /// Quand les deux ont confirmé, le Host envoie StartGameParams et tout le monde charge la scène de jeu.
    /// À placer sur un GameObject avec NetworkObject, spawné par le Host après StartHost.
    /// </summary>
    public class LobbyNetworkState : NetworkBehaviour
    {
        public const int DeckNotSet = -1;

        private NetworkVariable<int> _hostDeckChoice = new NetworkVariable<int>(DeckNotSet);
        private NetworkVariable<int> _clientDeckChoice = new NetworkVariable<int>(DeckNotSet);
        private bool _launchSent;

        public static event Action<StartGameParams> OnLaunchGame;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _hostDeckChoice.Value = DeckNotSet;
                _clientDeckChoice.Value = DeckNotSet;
            }
            _hostDeckChoice.OnValueChanged += (_, __) => CheckLaunch();
            _clientDeckChoice.OnValueChanged += (_, __) => CheckLaunch();
            CheckLaunch();
        }

        private void CheckLaunch()
        {
            if (!IsServer || _launchSent) return;
            if (_hostDeckChoice.Value == DeckNotSet || _clientDeckChoice.Value == DeckNotSet) return;
            _launchSent = true;
            var first = UnityEngine.Random.Range(0, 2);
            int seed = Environment.TickCount;
            var p = StartGameParams.Create(first, (DeckKind)_hostDeckChoice.Value, (DeckKind)_clientDeckChoice.Value, seed);
            LaunchGameClientRpc(p);
        }

        [ClientRpc]
        private void LaunchGameClientRpc(StartGameParams p)
        {
            // Le Host dépile proprement l'objet réseau du lobby avant de changer de scène,
            // pour éviter les Destroy côté client non-host.
            if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }

            NetworkGameParamsHolder.Params = p;
            NetworkGameParamsHolder.IsHost = NetworkManager.Singleton.IsHost;
            SceneManager.LoadScene(MenuController.SceneNames.MultiplayeurBoard);
        }

        /// <summary>Appelé par le Host quand il confirme son deck (Joueur 1).</summary>
        public void SetHostDeck(DeckKind deck)
        {
            if (!IsServer) return;
            _hostDeckChoice.Value = (int)deck;
        }

        /// <summary>Appelé par le Client via RPC quand il confirme son deck (Joueur 2).</summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetClientDeckServerRpc(int deck)
        {
            if (deck < 0 || deck > 1) return; // DeckKind: 0 = Magicien, 1 = Guerrier
            _clientDeckChoice.Value = deck;
        }

        public int HostDeckChoice => _hostDeckChoice.Value;
        public int ClientDeckChoice => _clientDeckChoice.Value;
    }
}
