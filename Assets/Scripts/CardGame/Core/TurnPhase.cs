namespace CardGame.Core
{
    /// <summary>
    /// Phase du tour selon carte_spec_complete.md.
    /// </summary>
    public enum TurnPhase
    {
        StartTurn,           // Défausse main, bouclier à 0
        ResolveStartOfTurn,  // Effets début de tour
        Draw,                // Pioche
        Play,                // Le joueur joue (cartes, frappe)
        Reaction,            // Fenêtre pour cartes Rapides (avant résolution dégâts)
        ResolveEndOfTurn,    // Effets fin de tour
        EndTurn              // Fin du tour
    }
}
