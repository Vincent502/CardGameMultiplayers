using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>Mode de partie pour le tri des stats.</summary>
    public enum StatsBlockKind
    {
        Global,
        Solo,
        MultiPlayer,
        Ghost
    }

    /// <summary>
    /// Bloc de stats réutilisable (parties, records, cumuls, cartes).
    /// </summary>
    [Serializable]
    public class StatsBlock
    {
        public PartiesData parties = new PartiesData();
        public RecordsData records = new RecordsData();
        public CumulsData cumuls = new CumulsData();
        public List<CardCount> cartes = new List<CardCount>();
    }

    /// <summary>
    /// Modèle du profil joueur (JSON-serializable). Stocké dans Rapport/Profile/player_profile.json.
    /// Utilise des listes pour la sérialisation JsonUtility (pas de Dictionary).
    /// Global = tout combiné. Solo / Multi / Ghost = stats par type de partie.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public int version = 2;
        public string nom = "";
        public string dateCreation = "";
        public string lastUpdated = "";

        /// <summary>Stats globales (toutes parties combinées).</summary>
        public PartiesData parties = new PartiesData();
        public List<CardCount> cartes = new List<CardCount>();
        public RecordsData records = new RecordsData();
        public CumulsData cumuls = new CumulsData();

        /// <summary>Stats des parties solo (vs bot).</summary>
        public StatsBlock solo = new StatsBlock();
        /// <summary>Stats des parties multijoueur.</summary>
        public StatsBlock multi = new StatsBlock();
        /// <summary>Stats des parties abandonnées / déconnexion.</summary>
        public StatsBlock ghost = new StatsBlock();

        public List<string> succesDebloques = new List<string>();

        /// <summary>Retourne le bloc de stats correspondant au type demandé. Global = parties/records/cumuls/cartes à la racine.</summary>
        public StatsBlock GetStatsBlock(StatsBlockKind kind)
        {
            switch (kind)
            {
                case StatsBlockKind.Global:
                    return new StatsBlock { parties = parties, records = records, cumuls = cumuls, cartes = cartes ?? new List<CardCount>() };
                case StatsBlockKind.Solo:
                    return solo ?? new StatsBlock();
                case StatsBlockKind.MultiPlayer:
                    return multi ?? new StatsBlock();
                case StatsBlockKind.Ghost:
                    return ghost ?? new StatsBlock();
                default:
                    return new StatsBlock { parties = parties, records = records, cumuls = cumuls, cartes = cartes ?? new List<CardCount>() };
            }
        }

        /// <summary>Crée un profil vide avec le nom donné.</summary>
        public static PlayerProfile CreateNew(string nom)
        {
            var now = DateTime.UtcNow.ToString("O");
            return new PlayerProfile
            {
                nom = string.IsNullOrWhiteSpace(nom) ? "Joueur" : nom.Trim(),
                dateCreation = now,
                lastUpdated = now
            };
        }
    }

    [Serializable]
    public class CardCount
    {
        public string cardId;
        public int count;
    }

    [Serializable]
    public class PartiesData
    {
        public int total;
        public int gagnees;
        public int perdues;
        public int abandonnees;
        public List<DeckStatsEntry> parDeck = new List<DeckStatsEntry>();
    }

    [Serializable]
    public class DeckStatsEntry
    {
        public string deckName;
        public int jouees;
        public int gagnees;
    }


    [Serializable]
    public class RecordsData
    {
        public int maxDegatsUnTour;
        public int maxBouclierUnTour;
        public int maxBouclierGagneUnCoup;
        public int partieLaPlusLongue;
        public int partieLaPlusCourte;
        public PartieRecordTours partieRecordTours;
    }

    [Serializable]
    public class PartieRecordTours
    {
        public int tours;
        public string date;
        public string deckJoueur1;
        public string deckJoueur2;
        public string gagnant;
        public string resultatJoueur;
    }

    [Serializable]
    public class CumulsData
    {
        public int degatsInfliges;
        public int bouclierGagne;
        public int cartesPiochees;
        /// <summary>Victoires où le joueur a joué Contre-attaque (pour succès contre_maitre).</summary>
        public int victoiresAvecContreAttaque;
    }
}
