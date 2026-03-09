using System;
using CardGame.Core;
using Unity.Collections;
using Unity.Netcode;

namespace CardGame.Unity
{
    /// <summary>
    /// Paramètres de démarrage de partie envoyés par le Host au Client (Étape 3).
    /// Les deux appellent GameSession.StartGame avec ces paramètres pour le même état initial (lockstep).
    /// </summary>
    public struct StartGameParams : INetworkSerializable
    {
        public int FirstPlayerIndex;
        public int DeckJoueur1; // (int)DeckKind
        public int DeckJoueur2;
        public int Seed;
        public FixedString64Bytes HostPseudo;
        public FixedString64Bytes ClientPseudo;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref FirstPlayerIndex);
            serializer.SerializeValue(ref DeckJoueur1);
            serializer.SerializeValue(ref DeckJoueur2);
            serializer.SerializeValue(ref Seed);
            serializer.SerializeValue(ref HostPseudo);
            serializer.SerializeValue(ref ClientPseudo);
        }

        public static StartGameParams Create(int firstPlayerIndex, DeckKind deckJoueur1, DeckKind deckJoueur2, int seed, string hostPseudo, string clientPseudo)
        {
            return new StartGameParams
            {
                FirstPlayerIndex = firstPlayerIndex,
                DeckJoueur1 = (int)deckJoueur1,
                DeckJoueur2 = (int)deckJoueur2,
                Seed = seed,
                HostPseudo = string.IsNullOrWhiteSpace(hostPseudo) ? "Joueur 1" : new FixedString64Bytes(hostPseudo.Trim()),
                ClientPseudo = string.IsNullOrWhiteSpace(clientPseudo) ? "Joueur 2" : new FixedString64Bytes(clientPseudo.Trim())
            };
        }

        public DeckKind GetDeckJoueur1() => (DeckKind)DeckJoueur1;
        public DeckKind GetDeckJoueur2() => (DeckKind)DeckJoueur2;
        public string GetHostPseudo() => HostPseudo.ToString();
        public string GetClientPseudo() => ClientPseudo.ToString();
    }

    /// <summary>
    /// Stocke les paramètres reçus du Lobby pour la scène MultiplayeurBoard (lus par NetworkGameController).
    /// </summary>
    public static class NetworkGameParamsHolder
    {
        public static StartGameParams? Params;
        /// <summary>True si on est le Host (Joueur 1 côté réseau), false si Client (Joueur 2).</summary>
        public static bool IsHost;
        public static void Clear() { Params = null; }
    }
}
