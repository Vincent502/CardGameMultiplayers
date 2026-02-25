using System.Collections.Generic;

namespace CardGame.Core
{
    /// <summary>
    /// État d'un joueur (spec: PV 100, bouclier, Force, Résistance, mana, zones).
    /// </summary>
    public class PlayerState
    {
        public int PlayerIndex { get; set; }
        public bool IsHuman { get; set; }
        public DeckKind DeckKind { get; set; }

        public int PV { get; set; } = 100;
        public int Shield { get; set; }
        public int Force { get; set; }
        public int Resistance { get; set; }
        public int Mana { get; set; }
        /// <summary>Mana conservé pour jouer des Rapides pendant le tour adverse.</summary>
        public int ManaReservedForReaction { get; set; }

        public List<CardInstance> Hand { get; } = new List<CardInstance>();
        public List<CardInstance> Deck { get; } = new List<CardInstance>();
        public List<CardInstance> Graveyard { get; } = new List<CardInstance>();
        public List<CardInstance> RemovedFromGame { get; } = new List<CardInstance>();
        public List<EquipmentState> Equipments { get; } = new List<EquipmentState>();

        /// <summary>Cartes défaussées ce tour (main défaussée en début de tour).</summary>
        public List<CardInstance> CardsDiscardedThisTurn { get; } = new List<CardInstance>();
        /// <summary>Nombre de cartes Éphémère consommées ce tour/round (passif Magicien).</summary>
        public int EphemeralConsumedThisRound { get; set; }
        /// <summary>Nombre de cartes Éphémère consommées depuis le début de la partie (Explosion magie éphémère).</summary>
        public int EphemeralConsumedThisGame { get; set; }
        /// <summary>Discipline éternel jouée → Souffle éternel va au cimetière.</summary>
        public bool HasPlayedDisciplineEternelThisGame { get; set; }
        /// <summary>Cartes Éphémère déjà utilisées cette partie.</summary>
        public HashSet<CardId> EphemereUsed { get; } = new HashSet<CardId>();
        /// <summary>Invincible jusqu'au prochain tour (Discipline éternel).</summary>
        public bool InvincibleUntilNextTurn { get; set; }
        /// <summary>Frappes consécutives ce tour (Rune protection Guerrier).</summary>
        public int ConsecutiveStrikesThisTurn { get; set; }
        /// <summary>Une attaque (frappe ou dégâts) a été faite ce tour (Défense Magicien).</summary>
        public bool AttackDoneThisTurn { get; set; }
        /// <summary>Repositionnement joué ce tour (1 seule fois par tour).</summary>
        public bool HasPlayedRepositionnementThisTurn { get; set; }

        /// <summary>Modificateurs temporaires (ex. Concentration) : (valeur, tours restants).</summary>
        public int ForceBonusTurnsLeft { get; set; }
        public int ForceBonusValue { get; set; }
        public int ResistanceBonusTurnsLeft { get; set; }
        public int ResistanceBonusValue { get; set; }
        /// <summary>Bonus de dégâts d'arme ce tour (ex. Appuis solide +1). Remis à 0 en fin de tour.</summary>
        public int WeaponDamageBonusThisTurn { get; set; }
    }
}
