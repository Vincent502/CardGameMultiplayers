using System;
using CardGame.Core;
using Unity.Collections;
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
        private NetworkVariable<FixedString64Bytes> _hostPseudo = new NetworkVariable<FixedString64Bytes>();
        private NetworkVariable<FixedString64Bytes> _clientPseudo = new NetworkVariable<FixedString64Bytes>();
        private bool _launchSent;

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
            if (_hostPseudo.Value.Length == 0 || _clientPseudo.Value.Length == 0) return;
            _launchSent = true;
            var first = UnityEngine.Random.Range(0, 2);
            int seed = Environment.TickCount;
            var p = StartGameParams.Create(first, (DeckKind)_hostDeckChoice.Value, (DeckKind)_clientDeckChoice.Value, seed, _hostPseudo.Value.ToString(), _clientPseudo.Value.ToString());
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
        public void SetHostDeck(DeckKind deck, string pseudo)
        {
            if (!IsServer) return;
            _hostPseudo.Value = new FixedString64Bytes(!string.IsNullOrWhiteSpace(pseudo) ? pseudo.Trim() : "Joueur 1");
            _hostDeckChoice.Value = (int)deck; // Déclenche CheckLaunch après que le pseudo soit défini
        }

        /// <summary>Appelé par le Client via RPC quand il confirme son deck (Joueur 2).</summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetClientDeckServerRpc(int deck, FixedString64Bytes pseudo)
        {
            if (deck < 0 || deck > 1) return; // DeckKind: 0 = Magicien, 1 = Guerrier
            _clientPseudo.Value = pseudo.Length > 0 ? pseudo : new FixedString64Bytes("Joueur 2");
            _clientDeckChoice.Value = deck; // Déclenche CheckLaunch après que le pseudo soit défini
        }

        public int HostDeckChoice => _hostDeckChoice.Value;
        public int ClientDeckChoice => _clientDeckChoice.Value;
        /// <summary>Pseudo du Host (Joueur 1), vide tant qu'il n'a pas confirmé.</summary>
        public string HostPseudo => _hostPseudo.Value.Length > 0 ? _hostPseudo.Value.ToString() : null;
        /// <summary>Pseudo du Client (Joueur 2), vide tant qu'il n'a pas confirmé.</summary>
        public string ClientPseudo => _clientPseudo.Value.Length > 0 ? _clientPseudo.Value.ToString() : null;
    }
}
