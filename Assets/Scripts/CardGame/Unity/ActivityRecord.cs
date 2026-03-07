using System;

namespace CardGame.Unity
{
    /// <summary>
    /// Modèle d'une entrée d'activité enregistrée dans le journal de partie.
    /// </summary>
    [Serializable]
    public class ActivityRecord
    {
        public int Seq;
        public string Time;
        public int Turn;
        public string EventType;
        public ActivityDetail Detail;

        /// <summary>Heure courte pour affichage (HH:mm:ss).</summary>
        public string TimeShort => !string.IsNullOrEmpty(Time) && Time.Length > 19 ? Time.Substring(11, 8) : Time ?? "";
    }

    /// <summary>
    /// Détails structurés d'une activité. Champs optionnels selon le type d'événement.
    /// </summary>
    [Serializable]
    public class ActivityDetail
    {
        // Joueurs
        public string joueur;
        public string attaquant;
        public string defenseur;
        public string lanceur;
        public string cible;
        public string frappeur;
        public string gagnant;
        public string firstPlayer;

        // Cartes
        public string carte;
        public string cardId;
        public string equipement;
        public string equipementGele;
        public string carteRemise;
        public string source;
        public string effet;

        // Dégâts / bouclier / PV
        public int baseDamage;
        public int baseShield;
        public int amount;
        public int damageTotal;
        public int pvAvant;
        public int pvApres;
        public int shieldAvant;
        public int shieldApres;
        public int forceAvant;
        public int forceApres;
        public int resistanceAvant;
        public int resistanceApres;

        // Pioche / deck
        public int requested;
        public int drawn;
        public int deckRemaining;
        public int handCount;
        public int deckSize;
        public int cardsFromGraveyard;

        // Tours / mana
        public int turnNumber;
        public int turnCount;
        public int manaReserved;
        public int manaRecovered;
        public int pv;
        public int ephemeralConsumedThisRound;

        // Buffs / effets
        public int forceBonus;
        public int resistanceBonus;
        public int bonusDegatsArme;
        public int degatsParTour;
        public int forceRetiree;
        public int dureeTours;
        public string duree;
        public string reason;

        // Divers
        public int putBackIndex;
        public int winnerIndex;
        public string deckJoueur1;
        public string deckJoueur2;

        /// <summary>Texte lisible pour l'affichage selon le type d'événement.</summary>
        public string ToDisplayText(string eventType)
        {
            return eventType switch
            {
                "GameStart" => $"{firstPlayer} commence. Decks : {deckJoueur1} vs {deckJoueur2}",
                "StartTurn" => $"{joueur} — Tour {turnNumber}",
                "EndTurn" => $"{joueur} termine (mana réservée : {manaReserved}, PV : {pv})",
                "Draw" => $"{joueur} pioche {drawn} carte(s) (demandé : {requested}, deck : {deckRemaining}, main : {handCount})",
                "DeckReshuffled" => $"{joueur} — mélange {cardsFromGraveyard} carte(s) du cimetière, deck : {deckSize}",
                "PlayCard" => $"{joueur} joue {carte}",
                "PlayRapid" => $"{joueur} joue {carte} (rapide)",
                "RapidPlayed" => $"{joueur} joue {carte} (rapide)",
                "DamageApplied" => $"{cible} subit {baseDamage} dégâts de {source} (PV : {pvAvant} → {pvApres})",
                "DamageBlocked" => $"{cible} bloque les dégâts de {source} ({reason})",
                "ShieldApplied" => $"{cible} gagne {baseShield} bouclier ({source})",
                "ReactionPhase" => $"{attaquant} attaque {defenseur} avec {source}",
                "StrikeReactionPhase" => $"{frappeur} frappe {cible} (base : {baseDamage})",
                "NoReaction" => $"{defenseur} ne réagit pas à {attaquant} ({source})",
                "Victory" => $"{gagnant} gagne la partie (tour {turnCount})",
                "EquipmentUnfrozen" => $"{joueur} — {equipement} dégelé",
                "DivinationPutBack" => $"{joueur} remet {carteRemise} en position {putBackIndex}",
                "RuneEndurance" => $"{joueur} +3 PV (rune) : {pvAvant} → {pvApres}",
                "RuneAgressivite" => $"{joueur} +1 Force ({duree})",
                "ForceBonusExpired" => $"{joueur} — bonus Force expiré (-{forceRetiree}), Force : {forceApres}",
                "ShieldBuffExpired" => $"{joueur} — bouclier de {carte} expiré",
                "ResistanceBuffExpired" => $"{joueur} — résistance de {carte} expirée",
                "PassifMagicien" => $"{joueur} récupère {manaRecovered} mana (éphémères consommés : {ephemeralConsumedThisRound})",
                "Galvanisation" => $"{joueur} +{forceBonus} Force (main : {handCount})",
                "PositionOffensive" => $"{joueur} Force : {forceAvant} → {forceApres}",
                "PositionDefensive" => $"{joueur} Résistance : {resistanceAvant} → {resistanceApres}",
                "Concentration" => $"{joueur} +3 Force, +3 Résistance",
                "LienKarmique" => $"{joueur} +{resistanceBonus} Résistance ({dureeTours} tours)",
                "AppuisSolide" => $"{joueur} +{bonusDegatsArme} dégâts arme ({duree})",
                "OrageDePoche" => $"{lanceur} → {cible} : {degatsParTour} dégât/tour",
                "GlaceLocalisee" => $"{lanceur} gèle {equipementGele} de {cible}",
                "DisciplineEternel" => $"{joueur} — {effet}",
                "SouffleEternel" => $"{joueur} +15 PV : {pvAvant} → {pvApres}",
                "ArmurePsychique" => $"{joueur} +23 bouclier : {shieldAvant} → {shieldApres}",
                "EndTurnRequested" => $"{joueur} demande fin de tour ({turnNumber})",
                "EffectNotImplemented" => $"Effet non implémenté : {carte}",
                _ => null
            };
        }
    }
}
