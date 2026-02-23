using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Data;

namespace CardGame.Core
{
    /// <summary>
    /// Moteur de partie : tour, phases, résolution. Sans dépendance réseau.
    /// Utilise IGameLogger pour tout tracer. Conçu pour être piloté par Unity (ou bot) via SubmitAction.
    /// </summary>
    public class GameSession
    {
        public GameState State { get; }
        private readonly IGameLogger _log;
        private readonly EffectResolver _resolver;
        private int _nextInstanceId;

        public GameSession(IGameLogger log)
        {
            _log = log;
            State = new GameState();
            _resolver = new EffectResolver(log);
        }

        /// <summary>Démarre une partie. firstPlayerIndex = 0 ou 1 (tirage au sort côté appelant).</summary>
        public void StartGame(bool humanIsPlayer0, int firstPlayerIndex)
        {
            State.Players[0].IsHuman = humanIsPlayer0;
            State.Players[1].IsHuman = !humanIsPlayer0;
            State.FirstPlayerIndex = firstPlayerIndex;
            State.CurrentPlayerIndex = firstPlayerIndex;
            State.TurnCount = 0;
            State.Phase = TurnPhase.StartTurn;
            State.WinnerIndex = -1;

            BuildDecks(0, true);  // Magicien
            BuildDecks(1, false); // Guerrier

            _log.Log("GameStart", new { firstPlayerIndex, player0Deck = "Magicien", player1Deck = "Guerrier" });
        }

        private void BuildDecks(int playerIndex, bool magicien)
        {
            var deckDef = magicien ? DeckDefinitions.GetMagicienDeck() : DeckDefinitions.GetGuerrierDeck();
            var player = State.Players[playerIndex];
            player.PV = 100;
            player.Shield = 0;
            player.Force = 0;
            player.Resistance = 0;
            player.Hand.Clear();
            player.Deck.Clear();
            player.Graveyard.Clear();
            player.RemovedFromGame.Clear();
            player.Equipments.Clear();
            player.CardsDiscardedThisTurn.Clear();
            player.EphemereUsed.Clear();
            player.InvincibleUntilNextTurn = false;
            player.HasPlayedDisciplineEternelThisGame = false;

            foreach (var (card, count) in deckDef)
            {
                if (card.Type == CardType.Equipe)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var inst = NewCard(card.Id);
                        player.Equipments.Add(new EquipmentState { Card = inst, RoundsUntilActive = card.Cost, IsFrozen = false });
                    }
                }
                else
                {
                    for (int c = 0; c < count; c++)
                        player.Deck.Add(NewCard(card.Id));
                }
            }

            Shuffle(player.Deck);
        }

        private CardInstance NewCard(CardId id) => new CardInstance(id, _nextInstanceId++);
        private static readonly Random _rng = new Random();
        private static void Shuffle(List<CardInstance> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                var v = list[k];
                list[k] = list[n];
                list[n] = v;
            }
        }

        /// <summary>Avance d'une étape. Retourne NeedPlayAction / NeedReaction quand une action joueur est requise.</summary>
        public StepResult Step()
        {
            if (State.WinnerIndex >= 0) return StepResult.GameOver;

            switch (State.Phase)
            {
                case TurnPhase.StartTurn:
                    DoStartTurn();
                    return StepResult.PhaseAdvanced;
                case TurnPhase.ResolveStartOfTurn:
                    DoResolveStartOfTurn();
                    return StepResult.PhaseAdvanced;
                case TurnPhase.Draw:
                    DoDraw();
                    return StepResult.PhaseAdvanced;
                case TurnPhase.Play:
                    return StepResult.NeedPlayAction;
                case TurnPhase.ResolveEndOfTurn:
                    DoResolveEndOfTurn();
                    return StepResult.PhaseAdvanced;
                case TurnPhase.EndTurn:
                    DoEndTurn();
                    return StepResult.PhaseAdvanced;
                case TurnPhase.Reaction:
                    return StepResult.NeedReaction;
                default:
                    return StepResult.PhaseAdvanced;
            }
        }

        private void DoStartTurn()
        {
            var p = State.CurrentPlayer;
            p.CardsDiscardedThisTurn.Clear();
            foreach (var c in p.Hand)
                p.CardsDiscardedThisTurn.Add(c);
            p.Graveyard.AddRange(p.Hand);
            p.Hand.Clear();
            p.Shield = 0;
            p.AttackDoneThisTurn = false;
            p.ConsecutiveStrikesThisTurn = 0;
            _log.Log("StartTurn", new { playerIndex = State.CurrentPlayerIndex, turnNumber = State.GetCurrentTurnNumber(), discarded = p.CardsDiscardedThisTurn.Count });
            State.Phase = TurnPhase.ResolveStartOfTurn;
        }

        private void DoResolveStartOfTurn()
        {
            var p = State.CurrentPlayer;
            foreach (var eq in p.Equipments)
            {
                if (eq.RoundsUntilActive > 0) eq.RoundsUntilActive--;
                if (!eq.IsActive) continue;
                switch (eq.Card.Id)
                {
                    case CardId.RuneEnergieArcanique:
                        _resolver.DrawCards(State, State.CurrentPlayerIndex, 2, _log);
                        break;
                    case CardId.RuneEnduranceOublie:
                        p.PV = Math.Min(100, p.PV + 3);
                        _log.Log("RuneEndurance", new { playerIndex = State.CurrentPlayerIndex });
                        break;
                }
            }
            State.Phase = TurnPhase.Draw;
        }

        private void DoDraw()
        {
            int drawCount = State.GetDrawCountThisTurn();
            var p = State.CurrentPlayer;
            int drawn = 0;
            while (drawn < drawCount)
            {
                if (p.Deck.Count == 0)
                {
                    if (p.Graveyard.Count == 0) break;
                    p.Deck.AddRange(p.Graveyard);
                    p.Graveyard.Clear();
                    Shuffle(p.Deck);
                    _log.Log("DeckReshuffled", new { playerIndex = State.CurrentPlayerIndex });
                }
                if (p.Deck.Count == 0) break;
                var card = p.Deck[p.Deck.Count - 1];
                p.Deck.RemoveAt(p.Deck.Count - 1);
                p.Hand.Add(card);
                drawn++;
            }
            p.Mana = State.GetManaThisTurn();
            _log.Log("Draw", new { playerIndex = State.CurrentPlayerIndex, requested = drawCount, drawn, mana = p.Mana });
            State.Phase = TurnPhase.Play;
        }

        private void DoResolveEndOfTurn()
        {
            var p = State.CurrentPlayer;
            foreach (var eq in p.Equipments.Where(e => e.IsActive))
            {
                if (eq.Card.Id == CardId.RuneEssenceArcanique && p.Resistance == 0)
                    _resolver.ApplyShield(State, State.CurrentPlayerIndex, 5, "Rune essence arcanique");
            }
            State.Phase = TurnPhase.EndTurn;
        }

        private void DoEndTurn()
        {
            var p = State.CurrentPlayer;
            p.ManaReservedForReaction = p.Mana;
            _log.Log("EndTurn", new { playerIndex = State.CurrentPlayerIndex, manaReserved = p.Mana });
            State.TurnCount++;
            State.CurrentPlayerIndex = 1 - State.CurrentPlayerIndex;
            State.Phase = TurnPhase.StartTurn;
        }

        /// <summary>Valide et applique l'action du joueur. Retourne false si invalide.</summary>
        public bool SubmitAction(GameAction action)
        {
            if (State.WinnerIndex >= 0) return false;
            if (action.PlayerIndex != State.CurrentPlayerIndex) return false;

            if (action is PlayCardAction playCard)
                return TryPlayCard(playCard);
            if (action is StrikeAction)
                return TryStrike();
            if (action is EndTurnAction)
                return TryEndTurn();

            return false;
        }

        private bool TryPlayCard(PlayCardAction a)
        {
            var p = State.CurrentPlayer;
            if (a.HandIndex < 0 || a.HandIndex >= p.Hand.Count) return false;
            var card = p.Hand[a.HandIndex];
            var data = DeckDefinitions.GetCard(card.Id);

            if (data.Type == CardType.Rapide) return false; // Rapides en réaction uniquement
            int cost = data.Type == CardType.Equipe ? 0 : data.Cost;
            if (p.Mana < cost) return false;
            if (data.Type == CardType.Ephemere && p.EphemereUsed.Contains(card.Id)) return false;

            p.Mana -= cost;
            p.Hand.RemoveAt(a.HandIndex);

            bool toGraveyard = _resolver.ResolveCardEffect(State, card.Id, State.CurrentPlayerIndex, 1 - State.CurrentPlayerIndex, a.DivinationPutBackIndex);
            if (card.Id == CardId.Divination && a.DivinationPutBackIndex.HasValue)
            {
                int idx = p.Hand.Count - 2 + a.DivinationPutBackIndex.Value;
                if (idx >= 0 && idx < p.Hand.Count)
                {
                    var putBack = p.Hand[idx];
                    p.Hand.RemoveAt(idx);
                    p.Deck.Add(putBack);
                }
            }

            if (data.Type == CardType.Ephemere)
                p.EphemereUsed.Add(card.Id);
            if (toGraveyard)
                p.Graveyard.Add(card);
            else
                p.RemovedFromGame.Add(card);

            if (data.Type == CardType.Normal || (data.Type == CardType.Ephemere && toGraveyard))
                p.AttackDoneThisTurn = true; // carte portant dégâts compte comme attaque

            _log.Log("PlayCard", new { playerIndex = a.PlayerIndex, card = data.Name, handIndex = a.HandIndex });
            CheckVictory();
            return true;
        }

        private bool TryStrike()
        {
            int baseDmg = _resolver.GetWeaponBaseDamage(State, State.CurrentPlayerIndex);
            if (baseDmg <= 0) return false;

            _resolver.ResolveStrike(State, State.CurrentPlayerIndex, 1 - State.CurrentPlayerIndex);
            State.CurrentPlayer.AttackDoneThisTurn = true;
            State.CurrentPlayer.ConsecutiveStrikesThisTurn++;
            _log.Log("Strike", new { playerIndex = State.CurrentPlayerIndex });
            CheckVictory();
            return true;
        }

        private bool TryEndTurn()
        {
            State.Phase = TurnPhase.ResolveEndOfTurn;
            _log.Log("EndTurnRequested", new { playerIndex = State.CurrentPlayerIndex });
            return true;
        }

        private void CheckVictory()
        {
            for (int i = 0; i < 2; i++)
                if (State.Players[i].PV <= 0)
                {
                    State.WinnerIndex = 1 - i;
                    _log.Log("Victory", new { winnerIndex = State.WinnerIndex });
                }
        }
    }
}
