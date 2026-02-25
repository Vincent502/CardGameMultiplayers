using System.Collections.Generic;

namespace CardGame.Core
{
    /// <summary>
    /// État global de la partie : 2 joueurs, tour actuel, phase, premier joueur.
    /// </summary>
    public class GameState
    {
        public const int MaxPlayers = 2;
        /// <summary>Index du Joueur 1 (affiché) = toujours l'humain en solo.</summary>
        public const int Player1Index = 0;
        /// <summary>Index du Joueur 2 (affiché) = bot ou second humain en P2P.</summary>
        public const int Player2Index = 1;

        public PlayerState[] Players { get; } = new PlayerState[MaxPlayers];
        /// <summary>Effets actifs avec durée (ex. Orage de poche). 1 tour de jeu = 1 joueur.</summary>
        public List<ActiveDurationEffect> ActiveDurationEffects { get; } = new List<ActiveDurationEffect>();
        /// <summary>Nombre de tours de jeu écoulés (1 tour = 1 joueur).</summary>
        public int TurnCount { get; set; }
        /// <summary>Index du joueur dont c'est le tour (Player1Index ou Player2Index).</summary>
        public int CurrentPlayerIndex { get; set; }
        public TurnPhase Phase { get; set; }
        /// <summary>Index du joueur qui a commencé la partie (tirage au sort).</summary>
        public int FirstPlayerIndex { get; set; }
        /// <summary>Partie terminée : index du gagnant (Player1Index ou Player2Index), -1 si pas fini.</summary>
        public int WinnerIndex { get; set; } = -1;
        /// <summary>En phase Reaction : dégâts en attente. Null si pas en attente de réaction.</summary>
        public PendingReactionInfo PendingReaction { get; set; }
        /// <summary>En phase Reaction : index du défenseur (celui qui peut jouer Parade/Contre-attaque).</summary>
        public int ReactionTargetPlayerIndex { get; set; }
        /// <summary>Numéro affiché du gagnant (1 ou 2), -1 si partie non terminée.</summary>
        public int WinnerDisplayNumber => WinnerIndex < 0 ? -1 : WinnerIndex + 1;

        public GameState()
        {
            for (int i = 0; i < MaxPlayers; i++)
                Players[i] = new PlayerState { PlayerIndex = i };
        }

        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];
        public PlayerState Opponent => Players[CurrentPlayerIndex == Player1Index ? Player2Index : Player1Index];

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
