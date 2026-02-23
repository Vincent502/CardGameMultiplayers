namespace CardGame.Core
{
    /// <summary>
    /// Type de carte selon la spec (carte_spec_complete.md).
    /// </summary>
    public enum CardType
    {
        Equipe,   // Sur le board, effective jusqu'à la fin, activation après X rounds
        Normal,   // Va au cimetière après utilisation
        Ephemere, // Une fois par partie, retirée du jeu (sauf exception ex. Souffle éternel)
        Rapide    // Jouable pendant le tour adverse en réaction
    }
}
