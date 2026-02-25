using System;
using CardGame.Core;
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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref FirstPlayerIndex);
            serializer.SerializeValue(ref DeckJoueur1);
            serializer.SerializeValue(ref DeckJoueur2);
            serializer.SerializeValue(ref Seed);
        }

        public static StartGameParams Create(int firstPlayerIndex, DeckKind deckJoueur1, DeckKind deckJoueur2, int seed)
        {
            return new StartGameParams
            {
                FirstPlayerIndex = firstPlayerIndex,
                DeckJoueur1 = (int)deckJoueur1,
                DeckJoueur2 = (int)deckJoueur2,
                Seed = seed
            };
        }

        public DeckKind GetDeckJoueur1() => (DeckKind)DeckJoueur1;
        public DeckKind GetDeckJoueur2() => (DeckKind)DeckJoueur2;
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
