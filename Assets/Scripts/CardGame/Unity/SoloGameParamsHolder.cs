using CardGame.Core;

namespace CardGame.Unity
{
    /// <summary>
    /// Paramètres de démarrage pour le mode Solo : deck du joueur, deck du bot (aléatoire).
    /// </summary>
    public static class SoloGameParamsHolder
    {
        public static DeckKind? DeckJoueur1;
        public static DeckKind? DeckJoueur2;

        public static void Set(DeckKind humanDeck, DeckKind botDeck)
        {
            DeckJoueur1 = humanDeck;
            DeckJoueur2 = botDeck;
        }

        public static void Clear()
        {
            DeckJoueur1 = null;
            DeckJoueur2 = null;
        }
    }
}
