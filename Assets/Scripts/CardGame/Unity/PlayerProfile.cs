using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Modèle du profil joueur (JSON-serializable). Stocké dans Profile/player_profile.json.
    /// Utilise des listes pour la sérialisation JsonUtility (pas de Dictionary).
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public int version = 1;
        public string nom = "";
        public string dateCreation = "";
        public string lastUpdated = "";

        public PartiesData parties = new PartiesData();
        public List<CardCount> cartes = new List<CardCount>();
        public RecordsData records = new RecordsData();
        public CumulsData cumuls = new CumulsData();
        public List<string> succesDebloques = new List<string>();

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
