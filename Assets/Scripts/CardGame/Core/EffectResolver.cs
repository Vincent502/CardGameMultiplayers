using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Data;

namespace CardGame.Core
{
    /// <summary>
    /// Résolution des effets selon spec : dégâts = base × (1+Force), bouclier = base × (1+Résistance).
    /// Applique les effets par carte (CardId).
    /// </summary>
    public class EffectResolver
    {
        private readonly IGameLogger _log;

        public EffectResolver(IGameLogger log) => _log = log;

        /// <summary>Dégâts infligés = base × (1 + Force du caster).</summary>
        public int ComputeDamage(int baseDamage, int casterForce) =>
            (int)Math.Max(0, baseDamage * (1 + casterForce));

        /// <summary>Bouclier reçu = base × (1 + Résistance du cible).</summary>
        public int ComputeShield(int baseShield, int targetResistance) =>
            (int)Math.Max(0, baseShield * (1 + targetResistance));

        /// <summary>Applique les dégâts (bouclier puis PV). Invincible = 0 dégât.</summary>
        public void ApplyDamage(GameState state, int targetPlayerIndex, int baseDamage, int casterForce, string sourceName)
        {
            var target = state.Players[targetPlayerIndex];
            if (target.InvincibleUntilNextTurn)
            {
                _log.Log("DamageBlocked", new { targetPlayerIndex, source = sourceName, reason = "Invincible" });
                return;
            }
            int damage = ComputeDamage(baseDamage, casterForce);
            int toShield = Math.Min(damage, target.Shield);
            int toPV = damage - toShield;
            target.Shield -= toShield;
            target.PV = Math.Max(0, target.PV - toPV);
            _log.Log("DamageApplied", new { targetPlayerIndex, baseDamage, casterForce, damage, toShield, toPV, targetPV = target.PV, source = sourceName });
        }

        /// <summary>Glace localisée : jouer une carte qui fait des dégâts dégèle un équipement du joueur (celui qui a joué la carte).</summary>
        private void UnfreezeOneEquipmentIfAny(GameState state, int playerIndex)
        {
            var frozen = state.Players[playerIndex].Equipments.FirstOrDefault(e => e.IsFrozen);
            if (frozen != null)
            {
                frozen.IsFrozen = false;
                _log.Log("EquipmentUnfrozen", new { playerIndex, cardId = frozen.Card.Id.ToString(), reason = "carte dégâts" });
            }
        }

        /// <summary>Ajoute du bouclier (formule Résistance).</summary>
        public void ApplyShield(GameState state, int targetPlayerIndex, int baseShield, string sourceName)
        {
            var target = state.Players[targetPlayerIndex];
            int amount = ComputeShield(baseShield, target.Resistance);
            target.Shield += amount;
            _log.Log("ShieldApplied", new { targetPlayerIndex, baseShield, resistance = target.Resistance, amount, source = sourceName });
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
                    foreach (var c in player.Graveyard) player.Deck.Add(c);
                    player.Graveyard.Clear();
                    if (rng != null) Shuffle(player.Deck, rng);
                    log.Log("DeckReshuffled", new { playerIndex, cardsFromGraveyard = player.Deck.Count });
                }
                if (player.Deck.Count == 0) break;
                var card = player.Deck[player.Deck.Count - 1];
                player.Deck.RemoveAt(player.Deck.Count - 1);
                player.Hand.Add(card);
                drawn++;
            }
            log.Log("Draw", new { playerIndex, requested = count, drawn, deckRemaining = player.Deck.Count });
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
                    int consumed = caster.CardsDiscardedThisTurn.Count;
                    return (consumed * 2, 0);
                default: return (0, 0);
            }
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
                if (baseDmg > 0)
                {
                    pendingDamage = new PendingReactionInfo
                    {
                        TargetIndex = targetIndex,
                        AttackerIndex = casterIndex,
                        BaseDamage = baseDmg,
                        CasterForce = casterForce,
                        SourceName = data.Name,
                        UnfreezeAttacker = true
                    };
                }
            }

            switch (cardId)
            {
                case CardId.Attaque:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 5, caster.Force, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    return true;
                case CardId.AttaquePlus:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 9, caster.Force, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    return true;
                case CardId.BouleDeFeu:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 15, 0, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    return false;
                case CardId.AttaqueTactique:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 2, caster.Force, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    ApplyShield(state, casterIndex, 1, data.Name);
                    return true;
                case CardId.AttaqueLegere:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 3, caster.Force, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    ApplyShield(state, casterIndex, 2, data.Name);
                    return true;
                case CardId.AttaqueLourde:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 7, caster.Force, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    ApplyShield(state, casterIndex, 4, data.Name);
                    return true;
                case CardId.FendoireMortel:
                    if (pendingDamage == null) { ApplyDamage(state, targetIndex, 20, 0, data.Name); UnfreezeOneEquipmentIfAny(state, casterIndex); }
                    return false;
                case CardId.ExplosionMagieEphemere:
                    if (pendingDamage == null)
                    {
                        int consumed = caster.CardsDiscardedThisTurn.Count;
                        int dmg = consumed * 2;
                        ApplyDamage(state, targetIndex, dmg, 0, data.Name);
                        UnfreezeOneEquipmentIfAny(state, casterIndex);
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
                    _log.Log("Galvanisation", new { casterIndex, forceBonus });
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
                    _log.Log("PositionOffensive", new { casterIndex });
                    return false;
                case CardId.PositionDefensive:
                    caster.Resistance += 1;
                    _log.Log("PositionDefensive", new { casterIndex });
                    return false;
                case CardId.DisciplineEternel:
                    caster.InvincibleUntilNextTurn = true;
                    caster.HasPlayedDisciplineEternelThisGame = true;
                    _log.Log("DisciplineEternel", new { casterIndex });
                    return true;
                case CardId.SouffleEternel:
                    caster.PV = Math.Min(100, caster.PV + 15);
                    _log.Log("SouffleEternel", new { casterIndex, heal = 15 });
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
                    _log.Log("ArmurePsychique", new { casterIndex, turns = 2 });
                    return false;
                case CardId.Concentration:
                    caster.Force += 3;
                    caster.ForceBonusValue = 3;
                    caster.ForceBonusTurnsLeft = 1;
                    caster.ResistanceBonusValue = 3;
                    caster.ResistanceBonusTurnsLeft = 2; // prochain tour
                    _log.Log("Concentration", new { casterIndex });
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
                    _log.Log("LienKarmique", new { casterIndex, turns = 3 });
                    return true;
                case CardId.AppuisSolide:
                    caster.WeaponDamageBonusThisTurn += 1;
                    _log.Log("AppuisSolide", new { casterIndex });
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
                    _log.Log("OrageDePoche", new { casterIndex, targetIndex, turns = 3 });
                    return false;
                case CardId.GlaceLocalisee:
                    var targetEquipments = state.Players[targetIndex].Equipments;
                    var toFreeze = targetEquipments.FirstOrDefault(e => e.IsActive);
                    if (toFreeze != null)
                    {
                        toFreeze.IsFrozen = true;
                        _log.Log("GlaceLocalisee", new { casterIndex, targetIndex, equipment = DeckDefinitions.GetCard(toFreeze.Card.Id).Name });
                    }
                    return false;
                default:
                    _log.Log("EffectNotImplemented", new { cardId = cardId.ToString() });
                    return data.Type == CardType.Normal;
            }
        }

        /// <summary>Applique les dégâts en attente (NoReaction).</summary>
        public void ApplyPendingReaction(GameState state, PendingReactionInfo pending)
        {
            if (pending == null) return;
            ApplyDamage(state, pending.TargetIndex, pending.BaseDamage, pending.CasterForce, pending.SourceName);
            if (pending.UnfreezeAttacker)
                UnfreezeOneEquipmentIfAny(state, pending.AttackerIndex);
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
                        _log.Log("RuneAgressivite", new { strikerIndex = pending.AttackerIndex });
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
            _log.Log("RapidPlayed", new { cardId = cardId.ToString(), casterIndex, attackerIndex });
            if (cardId == CardId.ContreAttaque)
                ApplyDamage(state, attackerIndex, 2, 0, data.Name);
        }

        /// <summary>Dégâts de base de la frappe (Hache 5, Rune force arcanique 2) + bonus ce tour (ex. Appuis solide).</summary>
        public int GetWeaponBaseDamage(GameState state, int playerIndex)
        {
            int baseDmg = 0;
            var player = state.Players[playerIndex];
            foreach (var eq in player.Equipments.Where(e => e.IsActive))
            {
                if (eq.Card.Id == CardId.HacheOublie) baseDmg = 5;
                if (eq.Card.Id == CardId.RuneForceArcanique) baseDmg += 2;
            }
            return baseDmg + player.WeaponDamageBonusThisTurn;
        }

        /// <summary>
        /// Frappe : si seule arme gelée, un coup "brise le gel" (dégel sans dégâts). Sinon dégâts (arme × (1+Force)) + effets « à la frappe ».
        /// </summary>
        public void ResolveStrike(GameState state, int strikerIndex, int targetIndex)
        {
            var striker = state.Players[strikerIndex];
            int baseDmg = GetWeaponBaseDamage(state, strikerIndex);
            if (baseDmg <= 0)
            {
                var frozen = striker.Equipments.FirstOrDefault(e => e.IsFrozen);
                if (frozen != null)
                {
                    frozen.IsFrozen = false;
                    _log.Log("EquipmentUnfrozen", new { strikerIndex, cardId = frozen.Card.Id.ToString(), reason = "frappe briser le gel" });
                }
                return;
            }
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
                    _log.Log("RuneAgressivite", new { strikerIndex });
                }
            }
        }

        /// <summary>
        /// Résout les effets "avant fin de tour" (ex. Orage de poche) puis décrémente la durée de tous les effets.
        /// À appeler en fin de tour (DoResolveEndOfTurn).
        /// </summary>
        public void ResolveEndOfTurnEffects(GameState state)
        {
            foreach (var effect in state.ActiveDurationEffects.ToList())
            {
                if (effect.Kind == DurationEffectKind.DamageEachTurn)
                    ApplyDamage(state, effect.TargetPlayerIndex, effect.Value, 0, DeckDefinitions.GetCard(effect.CardId).Name);
            }
            for (int i = state.ActiveDurationEffects.Count - 1; i >= 0; i--)
            {
                var effect = state.ActiveDurationEffects[i];
                effect.TurnsRemaining--;
                if (effect.TurnsRemaining <= 0)
                {
                    if (effect.Kind == DurationEffectKind.ShieldBuff)
                    {
                        var target = state.Players[effect.TargetPlayerIndex];
                        target.Shield = Math.Max(0, target.Shield - effect.Value);
                        _log.Log("ShieldBuffExpired", new { cardId = effect.CardId.ToString(), targetPlayerIndex = effect.TargetPlayerIndex, value = effect.Value });
                    }
                    else if (effect.Kind == DurationEffectKind.ResistanceBuff)
                    {
                        var target = state.Players[effect.TargetPlayerIndex];
                        target.Resistance = Math.Max(0, target.Resistance - effect.Value);
                        _log.Log("ResistanceBuffExpired", new { cardId = effect.CardId.ToString(), targetPlayerIndex = effect.TargetPlayerIndex, value = effect.Value });
                    }
                    state.ActiveDurationEffects.RemoveAt(i);
                }
            }
        }
    }
}
