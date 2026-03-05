using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Data;

namespace CardGame.Core
{
    /// <summary>
    /// Résolution des effets selon spec : dégâts = base + Force, bouclier = base × (1+Résistance).
    /// Applique les effets par carte (CardId).
    /// </summary>
    public class EffectResolver
    {
        private readonly IGameLogger _log;

        public EffectResolver(IGameLogger log) => _log = log;

        /// <summary>Dégâts infligés = base + Force du caster.</summary>
        public int ComputeDamage(int baseDamage, int casterForce) =>
            (int)Math.Max(0, baseDamage + casterForce);

        /// <summary>Bouclier reçu = base × (1 + Résistance du cible).</summary>
        public int ComputeShield(int baseShield, int targetResistance) =>
            (int)Math.Max(0, baseShield * (1 + targetResistance));

        /// <summary>Applique les dégâts (bouclier puis PV). Invincible = 0 dégât.</summary>
        public void ApplyDamage(GameState state, int targetPlayerIndex, int baseDamage, int casterForce, string sourceName)
        {
            var target = state.Players[targetPlayerIndex];
            if (target.InvincibleUntilNextTurn)
            {
                _log.Log("DamageBlocked", new {
                cible = $"Joueur {targetPlayerIndex + 1}",
                source = sourceName,
                reason = "Invincible",
                targetPV = target.PV,
                targetShield = target.Shield,
                turnNumber = state.GetCurrentTurnNumber()
            });
                return;
            }
            int damage = ComputeDamage(baseDamage, casterForce);
            int toShield = Math.Min(damage, target.Shield);
            int toPV = damage - toShield;
            int pvAvant = target.PV;
            int shieldAvant = target.Shield;
            target.Shield -= toShield;
            target.PV = Math.Max(0, target.PV - toPV);
            _log.Log("DamageApplied", new {
                cible = $"Joueur {targetPlayerIndex + 1}",
                source = sourceName,
                baseDamage,
                casterForce,
                damageTotal = damage,
                toShield,
                toPV,
                pvAvant,
                pvApres = target.PV,
                shieldAvant,
                shieldApres = target.Shield,
                turnNumber = state.GetCurrentTurnNumber()
            });
        }

        /// <summary>Glace localisée : le gel ne se retire que par le passage des tours (2 tours du joueur propriétaire), pas par frappe ni carte dégâts.</summary>

        /// <summary>Ajoute du bouclier (formule Résistance).</summary>
        public void ApplyShield(GameState state, int targetPlayerIndex, int baseShield, string sourceName)
        {
            var target = state.Players[targetPlayerIndex];
            int amount = ComputeShield(baseShield, target.Resistance);
            int shieldAvant = target.Shield;
            target.Shield += amount;
            _log.Log("ShieldApplied", new {
                cible = $"Joueur {targetPlayerIndex + 1}",
                source = sourceName,
                baseShield,
                resistance = target.Resistance,
                amount,
                shieldAvant,
                shieldApres = target.Shield,
                turnNumber = state.GetCurrentTurnNumber()
            });
        }

        /// <summary>Pioche n cartes (mélange cimetière si deck vide). Utilise rng pour un mélange déterministe (lockstep P2P).</summary>
        public void DrawCards(GameState state, int playerIndex, int count, IGameLogger log, Random rng)
        {
            var player = state.Players[playerIndex];
            int drawn = 0;
            while (drawn < count)
            {
                if (player.Deck.Count == 0)
                {
                    if (player.Graveyard.Count == 0) break;
                    int fromGraveyard = player.Graveyard.Count;
                    foreach (var c in player.Graveyard) player.Deck.Add(c);
                    player.Graveyard.Clear();
                    if (rng != null) Shuffle(player.Deck, rng);
                    log.Log("DeckReshuffled", new { joueur = $"Joueur {playerIndex + 1}", cardsFromGraveyard = fromGraveyard, deckSize = player.Deck.Count, turnNumber = state.GetCurrentTurnNumber() });
                }
                if (player.Deck.Count == 0) break;
                var card = player.Deck[player.Deck.Count - 1];
                player.Deck.RemoveAt(player.Deck.Count - 1);
                player.Hand.Add(card);
                drawn++;
            }
            log.Log("Draw", new { joueur = $"Joueur {playerIndex + 1}", requested = count, drawn, deckRemaining = player.Deck.Count, handCount = player.Hand.Count, turnNumber = state.GetCurrentTurnNumber() });
        }

        private static void Shuffle(List<CardInstance> list, Random rng)
        {
            if (rng == null) return;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var v = list[k];
                list[k] = list[n];
                list[n] = v;
            }
        }

        /// <summary>Paramètres de dégâts pour une carte (utilisé pour différer en phase Réaction).</summary>
        public (int baseDamage, int casterForce) GetDamageParamsForCard(CardId cardId, int casterIndex, GameState state)
        {
            var caster = state.Players[casterIndex];
            switch (cardId)
            {
                case CardId.Attaque: return (5, caster.Force);
                case CardId.AttaquePlus: return (9, caster.Force);
                case CardId.BouleDeFeu: return (15, 0);
                case CardId.AttaqueTactique: return (2, caster.Force);
                case CardId.AttaqueLegere: return (3, caster.Force);
                case CardId.AttaqueLourde: return (7, caster.Force);
                case CardId.FendoireMortel: return (20, 0);
                case CardId.ExplosionMagieEphemere:
                    int ephemeralConsumed = caster.EphemeralConsumedThisGame;
                    return (ephemeralConsumed * 2, 0);
                case CardId.Guillotine:
                    int weaponBase = GetWeaponBaseDamage(state, casterIndex);
                    return (weaponBase, caster.Force * 2);
                default: return (0, 0);
            }
        }

        /// <summary>Applique dégâts d'une carte + effet rune si équipée (2×(1+Force)).</summary>
        private void ApplyCardDamageWithRune(GameState state, int targetIndex, int casterIndex, int baseDamage, int casterForce, string sourceName)
        {
            ApplyDamage(state, targetIndex, baseDamage, casterForce, sourceName);
            ApplyRuneDamageIfEquipped(state, targetIndex, casterIndex, casterForce);
        }

        /// <summary>Applique 2×(1+Force) de la Rune de force arcanique si le caster l'a équipée.</summary>
        private void ApplyRuneDamageIfEquipped(GameState state, int targetIndex, int casterIndex, int casterForce)
        {
            if (!HasRuneForceArcanique(state, casterIndex)) return;
            for (int i = 0; i < 2; i++)
                ApplyDamage(state, targetIndex, 1, casterForce, "Rune de force arcanique");
        }

        /// <summary>True si la carte inflige des dégâts (susceptible d'être annulée par Parade/Contre-attaque).</summary>
        public bool CardDealsDamage(CardId cardId)
        {
            switch (cardId)
            {
                case CardId.Attaque:
                case CardId.AttaquePlus:
                case CardId.BouleDeFeu:
                case CardId.AttaqueTactique:
                case CardId.AttaqueLegere:
                case CardId.AttaqueLourde:
                case CardId.FendoireMortel:
                case CardId.ExplosionMagieEphemere:
                    return true;
                default: return false;
            }
        }

        /// <summary>Résout l'effet d'une carte jouée. Si deferDamage=true et carte à dégâts, ne fait pas ApplyDamage (retourne via out pendingDamage).</summary>
        public bool ResolveCardEffect(GameState state, CardId cardId, int casterIndex, int targetIndex, Random rng, int? divinationPutBackHandIndex, out PendingReactionInfo pendingDamage, bool deferDamage = false)
        {
            pendingDamage = null;
            var caster = state.Players[casterIndex];
            var target = state.Players[targetIndex];
            var data = DeckDefinitions.GetCard(cardId);

            if (deferDamage && CardDealsDamage(cardId))
            {
                var (baseDmg, casterForce) = GetDamageParamsForCard(cardId, casterIndex, state);
                int totalDamage = baseDmg + casterForce;
                if (totalDamage > 0)
                {
                    pendingDamage = new PendingReactionInfo
                    {
                        TargetIndex = targetIndex,
                        AttackerIndex = casterIndex,
                        BaseDamage = baseDmg,
                        CasterForce = casterForce,
                        SourceName = data.Name,
                        UnfreezeAttacker = false
                    };
                }
            }

            switch (cardId)
            {
                case CardId.Attaque:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 5, caster.Force, data.Name); }
                    return true;
                case CardId.AttaquePlus:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 9, caster.Force, data.Name); }
                    return true;
                case CardId.BouleDeFeu:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 15, 0, data.Name); }
                    return false;
                case CardId.AttaqueTactique:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 2, caster.Force, data.Name); }
                    ApplyShield(state, casterIndex, 1, data.Name);
                    return true;
                case CardId.AttaqueLegere:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 3, caster.Force, data.Name); }
                    ApplyShield(state, casterIndex, 2, data.Name);
                    return true;
                case CardId.AttaqueLourde:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 7, caster.Force, data.Name); }
                    ApplyShield(state, casterIndex, 4, data.Name);
                    return true;
                case CardId.FendoireMortel:
                    if (pendingDamage == null) { ApplyCardDamageWithRune(state, targetIndex, casterIndex, 20, 0, data.Name); }
                    return false;
                case CardId.ExplosionMagieEphemere:
                    if (pendingDamage == null)
                    {
                        int ephemeralConsumed = caster.EphemeralConsumedThisGame;
                        int dmg = ephemeralConsumed * 2;
                        ApplyCardDamageWithRune(state, targetIndex, casterIndex, dmg, 0, data.Name);
                    }
                    return true;
                case CardId.Guillotine:
                    if (pendingDamage == null)
                    {
                        int weaponBase = GetWeaponOnlyBase(state, casterIndex);
                        ApplyDamage(state, targetIndex, weaponBase, caster.Force * 2, data.Name);
                        ApplyRuneDamageIfEquipped(state, targetIndex, casterIndex, caster.Force);
                    }
                    return true;
                case CardId.Defense:
                    ApplyShield(state, casterIndex, 4, data.Name);
                    // +4 si aucune attaque ce tour : vérifié en fin de tour
                    return true;
                case CardId.DefensePlus:
                    ApplyShield(state, casterIndex, 15, data.Name);
                    return true;
                case CardId.Galvanisation:
                    int forceBonus = caster.Hand.Count;
                    caster.Force += forceBonus;
                    caster.ForceBonusValue = forceBonus;
                    caster.ForceBonusTurnsLeft = 1; // jusqu'à fin du tour
                    _log.Log("Galvanisation", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    handCount = caster.Hand.Count,
                    forceBonus,
                    forceAvant = caster.Force - forceBonus,
                    forceApres = caster.Force,
                    duree = "jusqu'à fin du tour",
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return true;
                case CardId.Evaluation:
                    DrawCards(state, casterIndex, 3, _log, rng);
                    return false;
                case CardId.Divination:
                    DrawCards(state, casterIndex, 2, _log, rng);
                    // Le joueur choisit laquelle remettre sur le deck (divinationPutBackHandIndex = index dans les 2 dernières = 0 ou 1 dans hand après pioche). On traite dans GameSession après avoir reçu l'action.
                    return true;
                case CardId.Repositionnement:
                    ApplyShield(state, casterIndex, 2, data.Name);
                    DrawCards(state, casterIndex, 1, _log, rng);
                    return true;
                case CardId.DefenseLourde:
                    ApplyShield(state, casterIndex, 10, data.Name);
                    return true;
                case CardId.PositionOffensive:
                    caster.Force += 1;
                    _log.Log("PositionOffensive", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    forceAvant = caster.Force - 1,
                    forceApres = caster.Force,
                    bonus = 1,
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return false;
                case CardId.PositionDefensive:
                    caster.Resistance += 1;
                    _log.Log("PositionDefensive", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    resistanceAvant = caster.Resistance - 1,
                    resistanceApres = caster.Resistance,
                    bonus = 1,
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return false;
                case CardId.DisciplineEternel:
                    caster.InvincibleUntilNextTurn = true;
                    caster.HasPlayedDisciplineEternelThisGame = true;
                    _log.Log("DisciplineEternel", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    effet = "Invincible jusqu'au prochain tour",
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return true;
                case CardId.SouffleEternel:
                    caster.PV = Math.Min(100, caster.PV + 15);
                    _log.Log("SouffleEternel", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    pvAvant = caster.PV - 15,
                    pvApres = caster.PV,
                    heal = 15,
                    toGraveyard = caster.HasPlayedDisciplineEternelThisGame,
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return caster.HasPlayedDisciplineEternelThisGame; // cimetière si Discipline jouée
                case CardId.ArmurePsychique:
                    ApplyShield(state, casterIndex, 23, data.Name);
                    state.ActiveDurationEffects.Add(new ActiveDurationEffect
                    {
                        CardId = CardId.ArmurePsychique,
                        Kind = DurationEffectKind.ShieldBuff,
                        CasterPlayerIndex = casterIndex,
                        TargetPlayerIndex = casterIndex,
                        TurnsRemaining = 2,
                        Value = 23
                    });
                    _log.Log("ArmurePsychique", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    shieldAvant = caster.Shield - 23,
                    shieldApres = caster.Shield,
                    amount = 23,
                    dureeTours = 2,
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return false;
                case CardId.Concentration:
                    caster.Force += 3;
                    caster.ForceBonusValue = 3;
                    caster.ForceBonusTurnsLeft = 1;
                    caster.ResistanceBonusValue = 3;
                    caster.ResistanceBonusTurnsLeft = 2; // prochain tour
                    _log.Log("Concentration", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    forceBonus = 3,
                    resistanceBonus = 3,
                    duree = "prochain tour",
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return false;
                case CardId.LienKarmique:
                    caster.Resistance += 3;
                    state.ActiveDurationEffects.Add(new ActiveDurationEffect
                    {
                        CardId = CardId.LienKarmique,
                        Kind = DurationEffectKind.ResistanceBuff,
                        CasterPlayerIndex = casterIndex,
                        TargetPlayerIndex = casterIndex,
                        TurnsRemaining = 3,
                        Value = 3
                    });
                    _log.Log("LienKarmique", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    resistanceBonus = 3,
                    dureeTours = 3,
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return true;
                case CardId.AppuisSolide:
                    caster.WeaponDamageBonusThisTurn += 1;
                    _log.Log("AppuisSolide", new {
                    joueur = $"Joueur {casterIndex + 1}",
                    bonusDegatsArme = 1,
                    duree = "ce tour",
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return false;
                case CardId.OrageDePoche:
                    state.ActiveDurationEffects.Add(new ActiveDurationEffect
                    {
                        CardId = CardId.OrageDePoche,
                        Kind = DurationEffectKind.DamageEachTurn,
                        CasterPlayerIndex = casterIndex,
                        TargetPlayerIndex = targetIndex,
                        TurnsRemaining = 3,
                        Value = 1
                    });
                    _log.Log("OrageDePoche", new {
                    lanceur = $"Joueur {casterIndex + 1}",
                    cible = $"Joueur {targetIndex + 1}",
                    degatsParTour = 1,
                    dureeTours = 3,
                    turnNumber = state.GetCurrentTurnNumber()
                });
                    return false;
                case CardId.GlaceLocalisee:
                    var targetEquipments = state.Players[targetIndex].Equipments;
                    var toFreeze = targetEquipments.FirstOrDefault(e => e.IsActive);
                    if (toFreeze != null)
                    {
                        toFreeze.IsFrozen = true;
                        toFreeze.FrozenTurnsRemaining = 2;
                        _log.Log("GlaceLocalisee", new {
                        lanceur = $"Joueur {casterIndex + 1}",
                        cible = $"Joueur {targetIndex + 1}",
                        equipementGele = DeckDefinitions.GetCard(toFreeze.Card.Id).Name,
                        cardId = toFreeze.Card.Id.ToString(),
                        duree = "2 tours du joueur propriétaire",
                        turnNumber = state.GetCurrentTurnNumber()
                    });
                    }
                    return false;
                default:
                    _log.Log("EffectNotImplemented", new { cardId = cardId.ToString(), carte = DeckDefinitions.GetCard(cardId).Name, turnNumber = state.GetCurrentTurnNumber() });
                    return data.Type == CardType.Normal;
            }
        }

        /// <summary>Applique les dégâts en attente (NoReaction). Rune = 2 attaques distinctes de (1+Force).</summary>
        public void ApplyPendingReaction(GameState state, PendingReactionInfo pending)
        {
            if (pending == null) return;
            if (pending.IsStrike)
            {
                if (pending.HasWeaponAttack)
                    ApplyDamage(state, pending.TargetIndex, pending.BaseDamage, pending.CasterForce, pending.SourceName);
                for (int i = 0; i < pending.RuneStrikeCount; i++)
                    ApplyDamage(state, pending.TargetIndex, 1, pending.CasterForce, "Rune de force arcanique");
            }
            else
            {
                ApplyDamage(state, pending.TargetIndex, pending.BaseDamage, pending.CasterForce, pending.SourceName);
                if (pending.AttackerIndex >= 0 && HasRuneForceArcanique(state, pending.AttackerIndex))
                    for (int i = 0; i < 2; i++)
                        ApplyDamage(state, pending.TargetIndex, 1, pending.CasterForce, "Rune de force arcanique");
            }
            if (pending.IsStrike)
            {
                var striker = state.Players[pending.AttackerIndex];
                foreach (var eq in striker.Equipments.Where(e => e.IsActive))
                {
                    if (eq.Card.Id == CardId.CatalyseurArcanaiqueRestraint)
                        ApplyShield(state, pending.AttackerIndex, 1, DeckDefinitions.GetCard(eq.Card.Id).Name);
                    if (eq.Card.Id == CardId.RuneAgressiviteOublie)
                    {
                        striker.Force += 1;
                        striker.ForceBonusValue += 1;
                        striker.ForceBonusTurnsLeft = Math.Max(striker.ForceBonusTurnsLeft, 1);
                        _log.Log("RuneAgressivite", new {
                        joueur = $"Joueur {pending.AttackerIndex + 1}",
                        forceBonus = 1,
                        duree = "jusqu'à fin du tour",
                        turnNumber = state.GetCurrentTurnNumber()
                    });
                    }
                }
                if (striker.ConsecutiveStrikesThisTurn == 2 && striker.Equipments.Any(e => e.IsActive && e.Card.Id == CardId.RuneProtectionOublie))
                    ApplyShield(state, pending.AttackerIndex, 2, "Rune de protection de l'oublié");
            }
        }

        /// <summary>Résout l'effet d'une carte Rapide (Contre-attaque, Parade). Annule l'attaque en attente. Contre-attaque inflige aussi 2 dégâts à l'attaquant.</summary>
        public void ResolveRapidCardEffect(GameState state, CardId cardId, int casterIndex, int attackerIndex)
        {
            var data = DeckDefinitions.GetCard(cardId);
            _log.Log("RapidPlayed", new {
            carte = DeckDefinitions.GetCard(cardId).Name,
            cardId = cardId.ToString(),
            joueur = $"Joueur {casterIndex + 1}",
            attaquant = $"Joueur {attackerIndex + 1}",
            type = cardId == CardId.ContreAttaque ? "Contre-attaque" : "Parade",
            turnNumber = state.GetCurrentTurnNumber()
        });
            if (cardId == CardId.ContreAttaque)
                ApplyDamage(state, attackerIndex, 2, 0, data.Name);
        }

        /// <summary>True si le joueur peut frapper (arme ou rune seule).</summary>
        public bool CanStrike(GameState state, int playerIndex)
        {
            return GetWeaponOnlyBase(state, playerIndex) > 0 || HasRuneForceArcanique(state, playerIndex);
        }

        /// <summary>Dégâts de base pour CanStrike : >0 si arme ou rune. Rune seule = 2 attaques de 1+Force.</summary>
        public int GetWeaponBaseDamage(GameState state, int playerIndex)
        {
            int weaponBase = GetWeaponOnlyBase(state, playerIndex);
            bool hasRune = HasRuneForceArcanique(state, playerIndex);
            if (hasRune && weaponBase == 0) return 2;
            if (hasRune) return weaponBase + 2;
            return weaponBase + state.Players[playerIndex].WeaponDamageBonusThisTurn;
        }

        /// <summary>True si le joueur a la Rune de force arcanique active (2 attaques de 1+Force).</summary>
        public bool HasRuneForceArcanique(GameState state, int playerIndex) =>
            state.Players[playerIndex].Equipments.Any(e => e.IsActive && e.Card.Id == CardId.RuneForceArcanique);

        /// <summary>Base de l'arme seule (Catalyseur 1, Hache 5), sans rune ni bonus.</summary>
        public int GetWeaponOnlyBase(GameState state, int playerIndex)
        {
            var player = state.Players[playerIndex];
            foreach (var eq in player.Equipments.Where(e => e.IsActive))
            {
                if (eq.Card.Id == CardId.CatalyseurArcanaiqueRestraint) return 1;
                if (eq.Card.Id == CardId.HacheOublie) return 5;
            }
            return 0;
        }

        /// <summary>
        /// Frappe : dégâts (arme + Force) + effets « à la frappe ». Le gel ne se retire que par le passage des tours.
        /// </summary>
        public void ResolveStrike(GameState state, int strikerIndex, int targetIndex)
        {
            var striker = state.Players[strikerIndex];
            int baseDmg = GetWeaponBaseDamage(state, strikerIndex);
            if (baseDmg <= 0) return;
            ApplyDamage(state, targetIndex, baseDmg, striker.Force, "Frappe");

            foreach (var eq in striker.Equipments.Where(e => e.IsActive))
            {
                if (eq.Card.Id == CardId.CatalyseurArcanaiqueRestraint)
                {
                    ApplyShield(state, strikerIndex, 1, DeckDefinitions.GetCard(eq.Card.Id).Name);
                }
                if (eq.Card.Id == CardId.RuneAgressiviteOublie)
                {
                    striker.Force += 1;
                    striker.ForceBonusValue += 1;
                    striker.ForceBonusTurnsLeft = Math.Max(striker.ForceBonusTurnsLeft, 1);
                    _log.Log("RuneAgressivite", new {
                    joueur = $"Joueur {strikerIndex + 1}",
                    forceBonus = 1,
                    duree = "jusqu'à fin du tour",
                    turnNumber = state.GetCurrentTurnNumber()
                });
                }
            }
        }

        /// <summary>
        /// Résout les effets "avant fin de tour" (ex. Orage de poche) puis décrémente la durée des effets.
        /// Les effets à durée (cartes) sont en "tours du joueur cible" : on ne décrémente que quand le joueur cible termine son tour.
        /// Les équipements restent en "tours de partie" (chaque tour de jeu compte).
        /// </summary>
        public void ResolveEndOfTurnEffects(GameState state, int currentPlayerIndex)
        {
            foreach (var effect in state.ActiveDurationEffects.ToList())
            {
                if (effect.Kind == DurationEffectKind.DamageEachTurn && effect.TargetPlayerIndex == currentPlayerIndex)
                    ApplyDamage(state, effect.TargetPlayerIndex, effect.Value, 0, DeckDefinitions.GetCard(effect.CardId).Name);
            }
            for (int i = state.ActiveDurationEffects.Count - 1; i >= 0; i--)
            {
                var effect = state.ActiveDurationEffects[i];
                if (effect.TargetPlayerIndex != currentPlayerIndex) continue;
                effect.TurnsRemaining--;
                if (effect.TurnsRemaining <= 0)
                {
                    if (effect.Kind == DurationEffectKind.ShieldBuff)
                    {
                        var target = state.Players[effect.TargetPlayerIndex];
                        target.Shield = Math.Max(0, target.Shield - effect.Value);
                        _log.Log("ShieldBuffExpired", new {
                        carte = DeckDefinitions.GetCard(effect.CardId).Name,
                        cardId = effect.CardId.ToString(),
                        joueur = $"Joueur {effect.TargetPlayerIndex + 1}",
                        bouclierRetire = effect.Value,
                        turnNumber = state.GetCurrentTurnNumber()
                    });
                    }
                    else if (effect.Kind == DurationEffectKind.ResistanceBuff)
                    {
                        var target = state.Players[effect.TargetPlayerIndex];
                        target.Resistance = Math.Max(0, target.Resistance - effect.Value);
                        _log.Log("ResistanceBuffExpired", new {
                        carte = DeckDefinitions.GetCard(effect.CardId).Name,
                        cardId = effect.CardId.ToString(),
                        joueur = $"Joueur {effect.TargetPlayerIndex + 1}",
                        resistanceRetiree = effect.Value,
                        turnNumber = state.GetCurrentTurnNumber()
                    });
                    }
                    state.ActiveDurationEffects.RemoveAt(i);
                }
            }
        }
    }
}
