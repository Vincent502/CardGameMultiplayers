namespace CardGame.Core
{
    /// <summary>
    /// État global de la partie : 2 joueurs, tour actuel, phase, premier joueur.
    /// </summary>
    public class GameState
    {
        public const int MaxPlayers = 2;

        public PlayerState[] Players { get; } = new PlayerState[MaxPlayers];
        /// <summary>Nombre de tours de jeu écoulés (1 tour = 1 joueur).</summary>
        public int TurnCount { get; set; }
        /// <summary>Joueur dont c'est le tour (0 ou 1).</summary>
        public int CurrentPlayerIndex { get; set; }
        public TurnPhase Phase { get; set; }
        /// <summary>Joueur qui a commencé la partie (tirage au sort).</summary>
        public int FirstPlayerIndex { get; set; }
        /// <summary>Partie terminée : gagnant (0 ou 1), -1 si pas fini.</summary>
        public int WinnerIndex { get; set; } = -1;

        public GameState()
        {
            for (int i = 0; i < MaxPlayers; i++)
                Players[i] = new PlayerState { PlayerIndex = i };
        }

        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];
        public PlayerState Opponent => Players[1 - CurrentPlayerIndex];

        /// <summary>Numéro de tour du joueur actuel (1, 2, 3...) pour mana/pioche.</summary>
        public int GetCurrentTurnNumber()
        {
            int turnNum = CurrentPlayerIndex == FirstPlayerIndex
                ? (TurnCount / 2 + 1)
                : ((TurnCount + 1) / 2);
            return turnNum;
        }

        /// <summary>Pioche ce tour (3/4/5, max 5).</summary>
        public int GetDrawCountThisTurn() => System.Math.Min(2 + GetCurrentTurnNumber(), 5);
        /// <summary>Mana ce tour (1/2/3, max 3).</summary>
        public int GetManaThisTurn() => System.Math.Min(GetCurrentTurnNumber(), 3);
    }
}
