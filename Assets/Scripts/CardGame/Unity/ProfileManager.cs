using System;
using System.Collections.Generic;
using System.IO;
using CardGame.Core;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Gère le profil joueur : existence, création, chargement, sauvegarde, fusion des stats.
    /// Fichier : Application.persistentDataPath/Profile/player_profile.json
    /// </summary>
    public static class ProfileManager
    {
        private static string ProfileDir => Path.Combine(Application.persistentDataPath, "Profile");
        private static string ProfilePath => Path.Combine(ProfileDir, "player_profile.json");

        /// <summary>True si le fichier profil existe et est valide.</summary>
        public static bool ProfilExiste()
        {
            if (string.IsNullOrEmpty(ProfilePath) || !File.Exists(ProfilePath))
                return false;
            try
            {
                string json = File.ReadAllText(ProfilePath);
                var p = JsonUtility.FromJson<PlayerProfile>(json);
                return p != null && !string.IsNullOrWhiteSpace(p.nom);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Crée un nouveau profil avec le nom donné et le sauvegarde.</summary>
        public static void CreerProfil(string nom)
        {
            var profile = PlayerProfile.CreateNew(nom);
            SaveProfile(profile);
        }

        /// <summary>Charge le profil depuis le fichier. Retourne null si absent ou invalide.</summary>
        public static PlayerProfile LoadProfile()
        {
            if (!File.Exists(ProfilePath)) return null;
            try
            {
                string json = File.ReadAllText(ProfilePath);
                return JsonUtility.FromJson<PlayerProfile>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProfileManager] Erreur chargement profil: {ex.Message}");
                return null;
            }
        }

        /// <summary>Sauvegarde le profil. Crée le dossier Profile si nécessaire.</summary>
        public static void SaveProfile(PlayerProfile profile)
        {
            if (profile == null) return;
            profile.lastUpdated = DateTime.UtcNow.ToString("O");
            try
            {
                if (!Directory.Exists(ProfileDir))
                    Directory.CreateDirectory(ProfileDir);
                string json = JsonUtility.ToJson(profile, true);
                File.WriteAllText(ProfilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileManager] Erreur sauvegarde profil: {ex.Message}");
            }
        }

        /// <summary>Enregistre un événement de partie (appelé par GameLogger).</summary>
        public static void OnGameEvent(SessionStats sessionStats, string eventType, string payloadJson)
        {
            if (sessionStats == null) return;
            sessionStats.Record(eventType, payloadJson);
        }

        /// <summary>Fusionne les stats de la partie terminée dans le profil et sauvegarde.</summary>
        public static void FinalizeGame(GameState state, SessionStats sessionStats)
        {
            if (sessionStats == null) return;
            var profile = LoadProfile();
            if (profile == null) return;
            if (profile.succesDebloques == null) profile.succesDebloques = new List<string>();

            sessionStats.SetTurnCount(state?.TurnCount ?? sessionStats.TurnCount);
            if (state != null && string.IsNullOrEmpty(sessionStats.DeckJoueur1) && state.Players.Length > 0)
                sessionStats.SetDeckJoueur1(state.Players[0].DeckKind.ToString());

            bool isVictory = state?.WinnerIndex == 0;
            bool isDefeat = state?.WinnerIndex == 1;
            if (isVictory) profile.parties.gagnees++;
            if (isDefeat) profile.parties.perdues++;
            profile.parties.total++;

            MergeDeckStats(profile, sessionStats, isVictory);
            MergeCartes(profile, sessionStats);
            MergeRecords(profile, sessionStats);
            MergeCumuls(profile, sessionStats);
            if (isVictory && sessionStats.ContreAttaqueJouee)
                profile.cumuls.victoiresAvecContreAttaque++;

            var newlyUnlocked = AchievementDefinition.CheckNewlyUnlocked(profile);
            foreach (var id in newlyUnlocked)
            {
                if (!profile.succesDebloques.Contains(id))
                    profile.succesDebloques.Add(id);
            }

            SaveProfile(profile);
        }

        /// <summary>Enregistre une partie abandonnée.</summary>
        public static void OnGameAbandoned(SessionStats sessionStats)
        {
            if (sessionStats == null) return;
            var profile = LoadProfile();
            if (profile == null) return;
            if (profile.succesDebloques == null) profile.succesDebloques = new List<string>();

            profile.parties.abandonnees++;
            profile.parties.total++;
            MergeDeckStats(profile, sessionStats, false);
            MergeCartes(profile, sessionStats);
            MergeCumuls(profile, sessionStats);

            var newlyUnlocked = AchievementDefinition.CheckNewlyUnlocked(profile);
            foreach (var id in newlyUnlocked)
            {
                if (!profile.succesDebloques.Contains(id))
                    profile.succesDebloques.Add(id);
            }

            SaveProfile(profile);
        }

        private static void MergeDeckStats(PlayerProfile profile, SessionStats s, bool isVictory)
        {
            string deck1 = s.DeckJoueur1 ?? "Magicien";
            var idx = profile.parties.parDeck.FindIndex(x => x.deckName == deck1);
            DeckStatsEntry entry;
            if (idx < 0)
            {
                entry = new DeckStatsEntry { deckName = deck1, jouees = 0, gagnees = 0 };
                profile.parties.parDeck.Add(entry);
            }
            else
            {
                entry = profile.parties.parDeck[idx];
            }
            entry.jouees++;
            if (isVictory) entry.gagnees++;
        }

        private static void MergeCartes(PlayerProfile profile, SessionStats s)
        {
            foreach (var kv in s.CartesJouees)
            {
                var idx = profile.cartes.FindIndex(x => x.cardId == kv.Key);
                if (idx < 0)
                {
                    profile.cartes.Add(new CardCount { cardId = kv.Key, count = kv.Value });
                }
                else
                {
                    profile.cartes[idx].count += kv.Value;
                }
            }
        }

        private static void MergeRecords(PlayerProfile profile, SessionStats s)
        {
            if (s.DegatsCeTour > profile.records.maxDegatsUnTour)
                profile.records.maxDegatsUnTour = s.DegatsCeTour;
            if (s.BouclierCeTour > profile.records.maxBouclierUnTour)
                profile.records.maxBouclierUnTour = s.BouclierCeTour;
            if (s.MaxBouclierUnCoup > profile.records.maxBouclierGagneUnCoup)
                profile.records.maxBouclierGagneUnCoup = s.MaxBouclierUnCoup;

            int turns = s.TurnCount;
            if (turns > 0)
            {
                if (turns > profile.records.partieLaPlusLongue)
                {
                    profile.records.partieLaPlusLongue = turns;
                    profile.records.partieRecordTours = new PartieRecordTours
                    {
                        tours = turns,
                        date = DateTime.UtcNow.ToString("O"),
                        deckJoueur1 = s.DeckJoueur1 ?? "",
                        deckJoueur2 = s.DeckJoueur2 ?? "",
                        gagnant = s.Gagnant ?? "",
                        resultatJoueur = s.WinnerIndex == 0 ? "gagné" : "perdu"
                    };
                }
                if (turns > 0 && (profile.records.partieLaPlusCourte == 0 || turns < profile.records.partieLaPlusCourte))
                    profile.records.partieLaPlusCourte = turns;
            }
        }

        private static void MergeCumuls(PlayerProfile profile, SessionStats s)
        {
            profile.cumuls.degatsInfliges += s.DegatsInfligesTotal;
            profile.cumuls.bouclierGagne += s.BouclierGagneTotal;
            profile.cumuls.cartesPiochees += s.CartesPiocheesTotal;
        }
    }
}
