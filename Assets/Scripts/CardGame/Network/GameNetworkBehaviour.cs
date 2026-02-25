using Unity.Netcode;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Reçoit et diffuse les actions de jeu en P2P (Host envoie en ClientRpc, Client envoie en ServerRpc).
    /// À placer sur un GameObject avec NetworkObject, spawné par le Host au chargement de MultiplayeurBoard.
    /// </summary>
    public class GameNetworkBehaviour : NetworkBehaviour
    {
        /// <summary>Appelé par le Host après avoir appliqué l'action localement : envoie au Client.</summary>
        public void SendActionToOtherClient(NetworkActionMessage msg)
        {
            if (!IsServer) return;
            ApplyActionClientRpc(msg);
        }

        [ClientRpc]
        private void ApplyActionClientRpc(NetworkActionMessage msg)
        {
            if (IsHost) return; // Host a déjà appliqué
            var ctrl = NetworkGameController.Instance;
            if (ctrl != null) ctrl.ApplyActionFromNetwork(msg);
        }

        /// <summary>Appelé par le Client après avoir appliqué l'action localement : le Host applique aussi.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReceiveFromClientServerRpc(NetworkActionMessage msg)
        {
            var ctrl = NetworkGameController.Instance;
            if (ctrl != null) ctrl.ApplyActionFromNetwork(msg);
        }
    }
}
