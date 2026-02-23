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

        private bool CanPlay(GameState state, int handIndex)
        {
            var p = state.CurrentPlayer;
            if (handIndex < 0 || handIndex >= p.Hand.Count) return false;
            var card = p.Hand[handIndex];
            var data = DeckDefinitions.GetCard(card.Id);
            if (data.Type == CardType.Rapide) return false;
            int cost = data.Type == CardType.Equipe ? 0 : data.Cost;
            if (p.Mana < cost) return false;
            if (data.Type == CardType.Ephemere && p.EphemereUsed.Contains(card.Id)) return false;
            return true;
        }
    }
}
