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

        /// <summary>Ajoute du bouclier (formule Résistance).</summary>
        public void ApplyShield(GameState state, int targetPlayerIndex, int baseShield, string sourceName)
        {
            var target = state.Players[targetPlayerIndex];
            int amount = ComputeShield(baseShield, target.Resistance);
            target.Shield += amount;
            _log.Log("ShieldApplied", new { targetPlayerIndex, baseShield, resistance = target.Resistance, amount, source = sourceName });
        }

        /// <summary>Pioche n cartes (mélange cimetière si deck vide).</summary>
        public void DrawCards(GameState state, int playerIndex, int count, IGameLogger log)
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
                    Shuffle(player.Deck);
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

        /// <summary>Résout l'effet d'une carte jouée. Retourne true si la carte va au cimetière (false = retirée du jeu Éphémère).</summary>
        public bool ResolveCardEffect(GameState state, CardId cardId, int casterIndex, int targetIndex, int? divinationPutBackHandIndex = null)
        {
            var caster = state.Players[casterIndex];
            var target = state.Players[targetIndex];
            var data = DeckDefinitions.GetCard(cardId);

            switch (cardId)
            {
                case CardId.Attaque:
                    ApplyDamage(state, targetIndex, 5, caster.Force, data.Name);
                    return true;
                case CardId.AttaquePlus:
                    ApplyDamage(state, targetIndex, 9, caster.Force, data.Name);
                    return true;
                case CardId.BouleDeFeu:
                    ApplyDamage(state, targetIndex, 15, 0, data.Name); // pas influencé par Force
                    return false; // Éphémère
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
                    DrawCards(state, casterIndex, 3, _log);
                    return false;
                case CardId.Divination:
                    DrawCards(state, casterIndex, 2, _log);
                    // Le joueur choisit laquelle remettre sur le deck (divinationPutBackHandIndex = index dans les 2 dernières = 0 ou 1 dans hand après pioche). On traite dans GameSession après avoir reçu l'action.
                    return true;
                case CardId.Repositionnement:
                    ApplyShield(state, casterIndex, 2, data.Name);
                    DrawCards(state, casterIndex, 1, _log);
                    return true;
                case CardId.AttaqueTactique:
                    ApplyDamage(state, targetIndex, 2, caster.Force, data.Name);
                    ApplyShield(state, casterIndex, 1, data.Name);
                    return true;
                case CardId.AttaqueLegere:
                    ApplyDamage(state, targetIndex, 3, caster.Force, data.Name);
                    ApplyShield(state, casterIndex, 2, data.Name);
                    return true;
                case CardId.AttaqueLourde:
                    ApplyDamage(state, targetIndex, 7, caster.Force, data.Name);
                    ApplyShield(state, casterIndex, 4, data.Name);
                    return true;
                case CardId.FendoireMortel:
                    ApplyDamage(state, targetIndex, 20, 0, data.Name);
                    return false;
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
                case CardId.ExplosionMagieEphemere:
                    int consumed = caster.CardsDiscardedThisTurn.Count;
                    int dmg = consumed * 2;
                    ApplyDamage(state, targetIndex, dmg, 0, data.Name);
                    return true;
                case CardId.ArmurePsychique:
                    ApplyShield(state, casterIndex, 23, data.Name); // TODO durée 2 tours
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
                    caster.Resistance += 3; // TODO durée 3 tours
                    _log.Log("LienKarmique", new { casterIndex });
                    return true;
                case CardId.AppuisSolide:
                    // +1 dégât arme : géré au moment de la frappe
                    _log.Log("AppuisSolide", new { casterIndex });
                    return false;
                default:
                    _log.Log("EffectNotImplemented", new { cardId = cardId.ToString() });
                    return data.Type == CardType.Normal;
            }
        }

        /// <summary>Dégâts de base de la frappe (Hache 5, Rune force arcanique 2).</summary>
        public int GetWeaponBaseDamage(GameState state, int playerIndex)
        {
            int baseDmg = 0;
            foreach (var eq in state.Players[playerIndex].Equipments.Where(e => e.IsActive))
            {
                if (eq.Card.Id == CardId.HacheOublie) baseDmg = 5;
                if (eq.Card.Id == CardId.RuneForceArcanique) baseDmg += 2;
            }
            return baseDmg;
        }

        /// <summary>Frappe : applique les dégâts (arme × (1+Force)).</summary>
        public void ResolveStrike(GameState state, int strikerIndex, int targetIndex)
        {
            int baseDmg = GetWeaponBaseDamage(state, strikerIndex);
            if (baseDmg <= 0) return;
            ApplyDamage(state, targetIndex, baseDmg, state.Players[strikerIndex].Force, "Frappe");
        }
    }
}
