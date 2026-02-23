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
        /// <summary>Générateur aléatoire déterministe (initialisé avec la graine au StartGame). Utilisé pour mélanges et tirages.</summary>
        private Random _rng;

        public GameSession(IGameLogger log)
        {
            _log = log;
            State = new GameState();
            _resolver = new EffectResolver(log);
        }

        /// <summary>
        /// Démarre une partie.
        /// humanIsJoueur1 = true si l'humain joue en tant que Joueur 1 (index 0).
        /// firstPlayerIndex = GameState.Player1Index ou Player2Index (tirage au sort côté appelant ou envoyé en P2P).
        /// deckJoueur1 / deckJoueur2 = choix de deck pour Joueur 1 et Joueur 2.
        /// seed = graine pour tout aléatoire (mélanges, etc.). Même seed → même état initial (lockstep P2P).
        /// </summary>
        public void StartGame(bool humanIsJoueur1, int firstPlayerIndex, DeckKind deckJoueur1, DeckKind deckJoueur2, int seed)
        {
            _rng = new Random(seed);
            State.Players[GameState.Player1Index].IsHuman = humanIsJoueur1;
            State.Players[GameState.Player2Index].IsHuman = !humanIsJoueur1;
            State.FirstPlayerIndex = firstPlayerIndex;
            State.CurrentPlayerIndex = firstPlayerIndex;
            State.TurnCount = 0;
            State.Phase = TurnPhase.StartTurn;
            State.WinnerIndex = -1;
            State.ActiveDurationEffects.Clear();

            BuildDecks(GameState.Player1Index, deckJoueur1);
            BuildDecks(GameState.Player2Index, deckJoueur2);

            _log.Log("GameStart", new { firstPlayerIndex, deckJoueur1 = deckJoueur1.ToString(), deckJoueur2 = deckJoueur2.ToString(), seed });
        }

        private void BuildDecks(int playerIndex, DeckKind deckKind)
        {
            var deckDef = deckKind == DeckKind.Magicien
                ? DeckDefinitions.GetMagicienDeck()
                : DeckDefinitions.GetGuerrierDeck();
            var player = State.Players[playerIndex];
            player.DeckKind = deckKind;
            player.PV = 100;
            player.Shield = 0;
            player.Force = deckKind == DeckKind.Guerrier ? 1 : 0;   // Passif Guerrier : +1 Force de base
            player.Resistance = deckKind == DeckKind.Guerrier ? 1 : 0; // Passif Guerrier : +1 Résistance de base
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

        private void Shuffle(List<CardInstance> list)
        {
            if (_rng == null) return;
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
                        _resolver.DrawCards(State, State.CurrentPlayerIndex, 2, _log, _rng);
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
            if (p.ForceBonusTurnsLeft > 0)
            {
                p.ForceBonusTurnsLeft--;
                if (p.ForceBonusTurnsLeft == 0)
                {
                    p.Force = Math.Max(0, p.Force - p.ForceBonusValue);
                    p.ForceBonusValue = 0;
                    _log.Log("ForceBonusExpired", new { playerIndex = State.CurrentPlayerIndex });
                }
            }
            _resolver.ResolveEndOfTurnEffects(State);
            // Glace localisée : dégel uniquement par carte dégâts ou frappe "briser le gel", pas en fin de tour.
            State.Phase = TurnPhase.EndTurn;
        }

        private void DoEndTurn()
        {
            var p = State.CurrentPlayer;
            p.ManaReservedForReaction = p.Mana;
            p.WeaponDamageBonusThisTurn = 0;
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
            // Éphémère : chaque exemplaire (instance) n'est jouable qu'une fois ; les autres exemplaires restent disponibles.

            p.Mana -= cost;
            p.Hand.RemoveAt(a.HandIndex);

            bool toGraveyard = _resolver.ResolveCardEffect(State, card.Id, State.CurrentPlayerIndex, 1 - State.CurrentPlayerIndex, _rng, a.DivinationPutBackIndex);
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

        /// <summary>True si le joueur actuel peut encore frapper (1 frappe max par tour : arme active ou arme gelée pour briser le gel).</summary>
        public bool CanStrike()
        {
            if (State == null || State.WinnerIndex >= 0) return false;
            if (State.Phase != TurnPhase.Play) return false;
            if (State.CurrentPlayer.ConsecutiveStrikesThisTurn >= 1) return false;
            var p = State.CurrentPlayer;
            return _resolver.GetWeaponBaseDamage(State, State.CurrentPlayerIndex) > 0 || p.Equipments.Any(e => e.IsFrozen);
        }

        private bool TryStrike()
        {
            if (State.CurrentPlayer.ConsecutiveStrikesThisTurn >= 1) return false;
            var p = State.CurrentPlayer;
            if (_resolver.GetWeaponBaseDamage(State, State.CurrentPlayerIndex) <= 0 && !p.Equipments.Any(e => e.IsFrozen)) return false;

            _resolver.ResolveStrike(State, State.CurrentPlayerIndex, 1 - State.CurrentPlayerIndex);
            p.AttackDoneThisTurn = true;
            p.ConsecutiveStrikesThisTurn++;
            if (p.ConsecutiveStrikesThisTurn == 2 && p.Equipments.Any(e => e.IsActive && e.Card.Id == CardId.RuneProtectionOublie))
            {
                _resolver.ApplyShield(State, State.CurrentPlayerIndex, 2, "Rune de protection de l'oublié");
            }
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
            for (int i = 0; i < GameState.MaxPlayers; i++)
                if (State.Players[i].PV <= 0)
                {
                    State.WinnerIndex = i == GameState.Player1Index ? GameState.Player2Index : GameState.Player1Index;
                    _log.Log("Victory", new { winnerIndex = State.WinnerIndex });
                }
        }
    }
}
