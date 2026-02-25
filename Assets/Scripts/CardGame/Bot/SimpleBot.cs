using System;
using System.Linq;
using CardGame.Core;
using CardGame.Data;

namespace CardGame.Bot
{
    /// <summary>
    /// Bot simple : joue une carte valide au hasard, ou frappe si possible, ou fin de tour.
    /// </summary>
    public class SimpleBot
    {
        private readonly Random _rng = new Random();

        /// <summary>Choisit une action pour le joueur actuel (bot).</summary>
        public GameAction ChooseAction(GameState state)
        {
            var p = state.CurrentPlayer;
            if (p.IsHuman) return null;

            // Option 1: Frappe si l'arme fait des dégâts (1 frappe max par tour, équipement "strike" une seule fois)
            int weaponDmg = 0;
            foreach (var eq in p.Equipments.Where(e => e.IsActive))
            {
                if (eq.Card.Id == CardId.HacheOublie) weaponDmg = 5;
                if (eq.Card.Id == CardId.RuneForceArcanique) weaponDmg += 2;
            }
            if (weaponDmg > 0 && p.ConsecutiveStrikesThisTurn == 0 && _rng.NextDouble() < 0.4)
                return new StrikeAction { PlayerIndex = state.CurrentPlayerIndex };

            // Option 2: Jouer une carte jouable
            var playable = Enumerable.Range(0, p.Hand.Count)
                .Where(i => CanPlay(state, i))
                .ToList();
            if (playable.Count > 0)
            {
                int idx = playable[_rng.Next(playable.Count)];
                var card = p.Hand[idx];
                var data = DeckDefinitions.GetCard(card.Id);
                int? div = null;
                if (card.Id == CardId.Divination && p.Hand.Count >= 2)
                    div = _rng.Next(2);
                return new PlayCardAction { PlayerIndex = state.CurrentPlayerIndex, HandIndex = idx, DivinationPutBackIndex = div };
            }

            // Option 3: Fin de tour
            return new EndTurnAction { PlayerIndex = state.CurrentPlayerIndex };
        }

        /// <summary>Choisit une action de réaction (Parade/Contre-attaque ou passer).</summary>
        public GameAction ChooseReactionAction(GameState state)
        {
            if (state.Phase != TurnPhase.Reaction || state.PendingReaction == null) return null;
            int defenderIdx = state.ReactionTargetPlayerIndex;
            var p = state.Players[defenderIdx];
            if (p.IsHuman) return null;

            var playableRapids = Enumerable.Range(0, p.Hand.Count)
                .Where(i =>
                {
                    var data = DeckDefinitions.GetCard(p.Hand[i].Id);
                    return data.Type == CardType.Rapide && p.ManaReservedForReaction >= data.Cost;
                })
                .ToList();
            if (playableRapids.Count > 0 && _rng.NextDouble() < 0.5)
            {
                int idx = playableRapids[_rng.Next(playableRapids.Count)];
                return new PlayRapidAction { PlayerIndex = defenderIdx, HandIndex = idx };
            }
            return new NoReactionAction { PlayerIndex = defenderIdx };
        }

        private bool CanPlay(GameState state, int handIndex)
        {
            var p = state.CurrentPlayer;
            if (handIndex < 0 || handIndex >= p.Hand.Count) return false;
            var card = p.Hand[handIndex];
            var data = DeckDefinitions.GetCard(card.Id);
            if (data.Type == CardType.Rapide) return false;
            int cost = data.Type == CardType.Equipe ? 0 : data.Cost;
            if (p.Mana < cost) return false;
            return true;
        }
    }
}
