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
        private bool _pendingDivinationChoice;

        /// <summary>True si on attend que le joueur choisisse laquelle des 2 cartes piochées par Divination remettre sur le deck.</summary>
        public bool PendingDivinationChoice => _pendingDivinationChoice;

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

            _log.Log("GameStart", new {
                firstPlayer = $"Joueur {firstPlayerIndex + 1}",
                deckJoueur1 = deckJoueur1.ToString(),
                deckJoueur2 = deckJoueur2.ToString(),
                seed,
                pvInitial = 100,
                turnCount = 0
            });
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
            player.EphemeralConsumedThisRound = 0;
            player.EphemeralConsumedThisGame = 0;
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
                    if (_pendingDivinationChoice) return StepResult.NeedDivinationChoice;
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
            foreach (var eq in p.Equipments.Where(e => e.IsFrozen))
            {
                eq.FrozenTurnsRemaining--;
                if (eq.FrozenTurnsRemaining <= 0)
                {
                    eq.IsFrozen = false;
                    eq.FrozenTurnsRemaining = 0;
                    _log.Log("EquipmentUnfrozen", new {
                    joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                    equipement = DeckDefinitions.GetCard(eq.Card.Id).Name,
                    cardId = eq.Card.Id.ToString(),
                    reason = "2 tours du joueur écoulés",
                    turnNumber = State.GetCurrentTurnNumber()
                });
                }
            }
            p.CardsDiscardedThisTurn.Clear();
            p.EphemeralConsumedThisRound = 0;
            foreach (var c in p.Hand)
                p.CardsDiscardedThisTurn.Add(c);
            p.Graveyard.AddRange(p.Hand);
            p.Hand.Clear();
            p.Shield = 0;
            p.AttackDoneThisTurn = false;
            p.ConsecutiveStrikesThisTurn = 0;
            p.HasPlayedRepositionnementThisTurn = false;
            State.EndTurnAfterReaction = false;
            _log.Log("StartTurn", new {
                joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                turnNumber = State.GetCurrentTurnNumber(),
                turnCount = State.TurnCount,
                discarded = p.CardsDiscardedThisTurn.Count,
                deckKind = p.DeckKind.ToString()
            });
            State.Phase = TurnPhase.ResolveStartOfTurn;
        }

        private void DoResolveStartOfTurn()
        {
            var p = State.CurrentPlayer;
            if (p.ResistanceBonusTurnsLeft == 2)
            {
                p.ResistanceBonusTurnsLeft = 1;
                p.Resistance += p.ResistanceBonusValue;
            }
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
                        _log.Log("RuneEndurance", new {
                        joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                        pvAvant = p.PV - 3,
                        pvApres = p.PV,
                        heal = 3,
                        turnNumber = State.GetCurrentTurnNumber()
                    });
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
                    int fromGraveyard = p.Graveyard.Count;
                    p.Deck.AddRange(p.Graveyard);
                    p.Graveyard.Clear();
                    Shuffle(p.Deck);
                    _log.Log("DeckReshuffled", new {
                        joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                        cardsFromGraveyard = fromGraveyard,
                        deckSize = p.Deck.Count,
                        turnNumber = State.GetCurrentTurnNumber()
                    });
                }
                if (p.Deck.Count == 0) break;
                var card = p.Deck[p.Deck.Count - 1];
                p.Deck.RemoveAt(p.Deck.Count - 1);
                p.Hand.Add(card);
                drawn++;
            }
            p.Mana = State.GetManaThisTurn();
            _log.Log("Draw", new {
                joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                requested = drawCount,
                drawn,
                deckRemaining = p.Deck.Count,
                handCount = p.Hand.Count,
                mana = p.Mana,
                turnNumber = State.GetCurrentTurnNumber()
            });
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
                    _log.Log("ForceBonusExpired", new {
                    joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                    forceRetiree = p.ForceBonusValue,
                    forceApres = p.Force,
                    turnNumber = State.GetCurrentTurnNumber()
                });
                }
            }
            if (p.ResistanceBonusTurnsLeft == 1)
            {
                int val = p.ResistanceBonusValue;
                p.ResistanceBonusTurnsLeft = 0;
                p.ResistanceBonusValue = 0;
                p.Resistance = Math.Max(0, p.Resistance - val);
            }
            _resolver.ResolveEndOfTurnEffects(State, State.CurrentPlayerIndex);
            // Glace localisée : dégel uniquement après 2 tours du joueur propriétaire (pas par frappe ni carte dégâts).
            State.Phase = TurnPhase.EndTurn;
        }

        private void DoEndTurn()
        {
            var p = State.CurrentPlayer;
            p.ManaReservedForReaction = p.Mana;
            p.WeaponDamageBonusThisTurn = 0;
            _log.Log("EndTurn", new {
                joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                manaReserved = p.Mana,
                pv = p.PV,
                shield = p.Shield,
                handCount = p.Hand.Count,
                turnNumber = State.GetCurrentTurnNumber()
            });
            State.TurnCount++;
            State.CurrentPlayerIndex = 1 - State.CurrentPlayerIndex;
            State.Phase = TurnPhase.StartTurn;
        }

        /// <summary>Valide et applique l'action du joueur. Retourne false si invalide.</summary>
        public bool SubmitAction(GameAction action)
        {
            if (State.WinnerIndex >= 0) return false;

            if (State.Phase == TurnPhase.Reaction)
            {
                if (action.PlayerIndex != State.ReactionTargetPlayerIndex) return false;
                if (action is PlayRapidAction rapid)
                    return TryPlayRapid(rapid);
                if (action is NoReactionAction)
                    return TryNoReaction();
                return false;
            }

            if (action.PlayerIndex != State.CurrentPlayerIndex) return false;
            if (action is PlayCardAction playCard)
                return TryPlayCard(playCard);
            if (action is DivinationPutBackAction divBack)
                return TryDivinationPutBack(divBack);
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
            if (card.Id == CardId.Repositionnement && p.HasPlayedRepositionnementThisTurn) return false;
            // Éphémère : chaque exemplaire (instance) n'est jouable qu'une fois ; les autres exemplaires restent disponibles.

            p.Mana -= cost;
            p.Hand.RemoveAt(a.HandIndex);

            int targetIndex = 1 - State.CurrentPlayerIndex;
            bool deferDamage = _resolver.CardDealsDamage(card.Id);
            bool toGraveyard = _resolver.ResolveCardEffect(State, card.Id, State.CurrentPlayerIndex, targetIndex, _rng, a.DivinationPutBackIndex, out var pendingDamage, deferDamage);
            if (card.Id == CardId.Divination)
            {
                if (a.DivinationPutBackIndex.HasValue)
                {
                    int idx = a.DivinationPutBackIndex.Value;
                    if (idx >= 0 && idx < p.Hand.Count)
                    {
                        var putBack = p.Hand[idx];
                        p.Hand.RemoveAt(idx);
                        p.Deck.Add(putBack);
                    }
                }
                else
                    _pendingDivinationChoice = true;
            }

            if (toGraveyard)
                p.Graveyard.Add(card);
            else
                p.RemovedFromGame.Add(card);

            if (card.Id == CardId.Repositionnement)
                p.HasPlayedRepositionnementThisTurn = true;

            if (data.Type == CardType.Normal || (data.Type == CardType.Ephemere && toGraveyard))
                p.AttackDoneThisTurn = true;

            // Cartes Éphémère consommées (RemovedFromGame)
            if (data.Type == CardType.Ephemere && !toGraveyard)
            {
                p.EphemeralConsumedThisRound++;
                p.EphemeralConsumedThisGame++;
                if (p.DeckKind == DeckKind.Magicien)
                {
                    p.Mana += 1;
                    _log.Log("PassifMagicien", new {
                    joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                    ephemeralConsumedThisRound = p.EphemeralConsumedThisRound,
                    manaRecovered = 1,
                    manaAvant = p.Mana - 1,
                    manaApres = p.Mana,
                    turnNumber = State.GetCurrentTurnNumber()
                });
                }
            }

            if (pendingDamage != null)
            {
                State.PendingReaction = pendingDamage;
                State.ReactionTargetPlayerIndex = targetIndex;
                State.Phase = TurnPhase.Reaction;
                if (card.Id == CardId.Guillotine)
                    State.EndTurnAfterReaction = true;
                _log.Log("ReactionPhase", new {
                attaquant = $"Joueur {State.CurrentPlayerIndex + 1}",
                defenseur = $"Joueur {targetIndex + 1}",
                source = data.Name,
                cardId = card.Id.ToString(),
                turnNumber = State.GetCurrentTurnNumber()
            });
            }
            else if (card.Id == CardId.Guillotine)
            {
                State.Phase = TurnPhase.ResolveEndOfTurn;
            }

            _log.Log("PlayCard", new {
                joueur = $"Joueur {a.PlayerIndex + 1}",
                carte = data.Name,
                cardId = card.Id.ToString(),
                handIndex = a.HandIndex,
                manaApres = p.Mana,
                handCount = p.Hand.Count,
                cible = $"Joueur {targetIndex + 1}",
                toGraveyard,
                turnNumber = State.GetCurrentTurnNumber()
            });
            CheckVictory();
            return true;
        }

        private bool TryPlayRapid(PlayRapidAction a)
        {
            if (State.Phase != TurnPhase.Reaction || State.PendingReaction == null) return false;
            var p = State.Players[State.ReactionTargetPlayerIndex];
            if (a.HandIndex < 0 || a.HandIndex >= p.Hand.Count) return false;
            var card = p.Hand[a.HandIndex];
            var data = DeckDefinitions.GetCard(card.Id);
            if (data.Type != CardType.Rapide) return false;
            int cost = data.Cost;
            if (p.ManaReservedForReaction < cost) return false;

            p.ManaReservedForReaction -= cost;
            p.Hand.RemoveAt(a.HandIndex);
            p.Graveyard.Add(card);

            var pending = State.PendingReaction;
            _resolver.ResolveRapidCardEffect(State, card.Id, State.ReactionTargetPlayerIndex, pending.AttackerIndex);

            // Boule de feu : Parade/Esquive ne réduisent que 50 % des dégâts
            if (card.Id == CardId.Parade &&
                pending != null &&
                pending.SourceName == DeckDefinitions.GetCard(CardId.BouleDeFeu).Name)
            {
                int total = _resolver.ComputeDamage(pending.BaseDamage, pending.CasterForce);
                int half = (total + 1) / 2;
                _resolver.ApplyDamage(State, pending.AttackerIndex, pending.TargetIndex, half, 0, pending.SourceName);
            }

            State.PendingReaction = null;
            State.Phase = State.EndTurnAfterReaction ? TurnPhase.ResolveEndOfTurn : TurnPhase.Play;
            State.EndTurnAfterReaction = false;
            _log.Log("PlayRapid", new {
                joueur = $"Joueur {a.PlayerIndex + 1}",
                carte = data.Name,
                cardId = card.Id.ToString(),
                type = "Parade ou Contre-attaque",
                turnNumber = State.GetCurrentTurnNumber()
            });
            CheckVictory();
            return true;
        }

        private bool TryNoReaction()
        {
            if (State.Phase != TurnPhase.Reaction || State.PendingReaction == null) return false;
            var pending = State.PendingReaction;
            _resolver.ApplyPendingReaction(State, pending);
            State.PendingReaction = null;
            State.Phase = State.EndTurnAfterReaction ? TurnPhase.ResolveEndOfTurn : TurnPhase.Play;
            State.EndTurnAfterReaction = false;
            _log.Log("NoReaction", new {
                defenseur = $"Joueur {State.ReactionTargetPlayerIndex + 1}",
                attaquant = $"Joueur {pending.AttackerIndex + 1}",
                source = pending.SourceName,
                baseDamage = pending.BaseDamage,
                casterForce = pending.CasterForce,
                turnNumber = State.GetCurrentTurnNumber()
            });
            CheckVictory();
            return true;
        }

        private bool TryDivinationPutBack(DivinationPutBackAction a)
        {
            if (!_pendingDivinationChoice) return false;
            var p = State.CurrentPlayer;
            if (a.PutBackIndex < 0 || a.PutBackIndex >= p.Hand.Count) return false;
            int idx = a.PutBackIndex;
            var putBack = p.Hand[idx];
            p.Hand.RemoveAt(idx);
            p.Deck.Add(putBack);
            _pendingDivinationChoice = false;
            _log.Log("DivinationPutBack", new {
                joueur = $"Joueur {a.PlayerIndex + 1}",
                putBackIndex = a.PutBackIndex,
                carteRemise = DeckDefinitions.GetCard(putBack.Id).Name,
                cardId = putBack.Id.ToString(),
                turnNumber = State.GetCurrentTurnNumber()
            });
            return true;
        }

        /// <summary>True si le joueur actuel peut encore frapper (1 frappe max par tour : arme active ou rune seule).</summary>
        public bool CanStrike()
        {
            if (State == null || State.WinnerIndex >= 0) return false;
            if (State.Phase != TurnPhase.Play) return false;
            if (State.CurrentPlayer.ConsecutiveStrikesThisTurn >= 1) return false;
            return _resolver.CanStrike(State, State.CurrentPlayerIndex);
        }

        private bool TryStrike()
        {
            if (State.CurrentPlayer.ConsecutiveStrikesThisTurn >= 1) return false;
            var p = State.CurrentPlayer;
            if (!_resolver.CanStrike(State, State.CurrentPlayerIndex)) return false;

            int weaponBase = _resolver.GetWeaponOnlyBase(State, State.CurrentPlayerIndex);
            bool hasRune = _resolver.HasRuneForceArcanique(State, State.CurrentPlayerIndex);
            int baseDmg = weaponBase > 0 ? weaponBase + p.WeaponDamageBonusThisTurn : 1;
            int runeCount = hasRune ? 2 : 0;
            bool hasWeaponAttack = weaponBase > 0;

            int targetIndex = 1 - State.CurrentPlayerIndex;
            State.PendingReaction = new PendingReactionInfo
            {
                TargetIndex = targetIndex,
                AttackerIndex = State.CurrentPlayerIndex,
                BaseDamage = baseDmg,
                CasterForce = p.Force,
                SourceName = "Frappe",
                UnfreezeAttacker = false,
                IsStrike = true,
                RuneStrikeCount = runeCount,
                HasWeaponAttack = hasWeaponAttack
            };
            State.ReactionTargetPlayerIndex = targetIndex;
            State.Phase = TurnPhase.Reaction;
            p.AttackDoneThisTurn = true;
            p.ConsecutiveStrikesThisTurn++;
            int dmgWeapon = hasWeaponAttack ? (baseDmg + p.Force) : 0;
            int dmgRune = runeCount * (1 + p.Force);
            _log.Log("StrikeReactionPhase", new {
                frappeur = $"Joueur {State.CurrentPlayerIndex + 1}",
                cible = $"Joueur {targetIndex + 1}",
                baseDamage = baseDmg,
                casterForce = p.Force,
                runeStrikes = runeCount,
                damageTotal = dmgWeapon + dmgRune,
                turnNumber = State.GetCurrentTurnNumber()
            });
            return true;
        }

        private bool TryEndTurn()
        {
            State.Phase = TurnPhase.ResolveEndOfTurn;
            _log.Log("EndTurnRequested", new {
                joueur = $"Joueur {State.CurrentPlayerIndex + 1}",
                turnNumber = State.GetCurrentTurnNumber()
            });
            return true;
        }

        private void CheckVictory()
        {
            for (int i = 0; i < GameState.MaxPlayers; i++)
                if (State.Players[i].PV <= 0)
                {
                    State.WinnerIndex = i == GameState.Player1Index ? GameState.Player2Index : GameState.Player1Index;
                    _log.Log("Victory", new {
                gagnant = $"Joueur {State.WinnerIndex + 1}",
                winnerIndex = State.WinnerIndex,
                turnCount = State.TurnCount,
                deckJoueur1 = State.Players[0].DeckKind.ToString(),
                deckJoueur2 = State.Players[1].DeckKind.ToString()
            });
                }
        }
    }
}
