using System;
using System.Collections.Generic;
using System.IO;
using CardGame.Core;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Gère le profil joueur : existence, création, chargement, sauvegarde, fusion des stats.
    /// Fichier : Application.persistentDataPath/Rapport/Profile/player_profile.json
    /// </summary>
    public static class ProfileManager
    {
        private static string ProfileDir => Path.Combine(Application.persistentDataPath, "Rapport", "Profile");
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
                var p = JsonUtility.FromJson<PlayerProfile>(json);
                if (p != null && p.version < 2 && p.parties.total > 0)
                    MigrateV1ToV2(p);
                return p;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProfileManager] Erreur chargement profil: {ex.Message}");
                return null;
            }
        }

        /// <summary>Migration v1 → v2 : les stats existantes sont considérées comme solo.</summary>
        private static void MigrateV1ToV2(PlayerProfile p)
        {
            p.version = 2;
            EnsureStatsBlocks(p);
            CopyBlockToBlock(p.parties, p.records, p.cumuls, p.cartes, p.solo);
        }

        private static void CopyBlockToBlock(PartiesData srcP, RecordsData srcR, CumulsData srcC, List<CardCount> srcCartes, StatsBlock dst)
        {
            if (dst == null) return;
            dst.parties = new PartiesData { total = srcP.total, gagnees = srcP.gagnees, perdues = srcP.perdues, abandonnees = srcP.abandonnees, parDeck = new List<DeckStatsEntry>(srcP.parDeck ?? new List<DeckStatsEntry>()) };
            dst.records = new RecordsData { maxDegatsUnTour = srcR.maxDegatsUnTour, maxBouclierUnTour = srcR.maxBouclierUnTour, maxBouclierGagneUnCoup = srcR.maxBouclierGagneUnCoup, partieLaPlusLongue = srcR.partieLaPlusLongue, partieLaPlusCourte = srcR.partieLaPlusCourte, partieRecordTours = srcR.partieRecordTours };
            dst.cumuls = new CumulsData { degatsInfliges = srcC.degatsInfliges, bouclierGagne = srcC.bouclierGagne, cartesPiochees = srcC.cartesPiochees, victoiresAvecContreAttaque = srcC.victoiresAvecContreAttaque };
            dst.cartes = srcCartes != null ? new List<CardCount>(srcCartes) : new List<CardCount>();
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
                Debug.Log($"[ProfileManager] Profil sauvegardé : {ProfilePath} (parties: {profile.parties.total})");
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

        /// <summary>Mode de partie pour le tri des stats.</summary>
        public enum GameMode { Solo, Multi }

        /// <summary>Fusionne les stats de la partie terminée dans le profil et sauvegarde.</summary>
        /// <param name="mode">Solo = vs bot, Multi = multijoueur.</param>
        public static void FinalizeGame(GameState state, SessionStats sessionStats, GameMode mode = GameMode.Solo)
        {
            if (sessionStats == null)
            {
                Debug.LogWarning("[ProfileManager] FinalizeGame ignoré : sessionStats null");
                return;
            }
            var profile = LoadProfile();
            if (profile == null)
            {
                profile = PlayerProfile.CreateNew("Joueur");
                Debug.Log("[ProfileManager] Aucun profil trouvé — création d'un profil par défaut pour enregistrer les stats.");
            }
            if (profile.succesDebloques == null) profile.succesDebloques = new List<string>();
            EnsureStatsBlocks(profile);

            sessionStats.SetTurnCount(state?.TurnCount ?? sessionStats.TurnCount);
            if (state != null && string.IsNullOrEmpty(sessionStats.DeckJoueur1) && state.Players.Length > 0)
                sessionStats.SetDeckJoueur1(state.Players[0].DeckKind.ToString());

            bool isVictory = state?.WinnerIndex == 0;
            bool isDefeat = state?.WinnerIndex == 1;

            var targetBlock = mode == GameMode.Solo ? profile.solo : profile.multi;
            MergeSessionIntoBlock(profile.parties, profile.records, profile.cumuls, profile.cartes, sessionStats, isVictory, false);
            MergeSessionIntoBlock(targetBlock.parties, targetBlock.records, targetBlock.cumuls, targetBlock.cartes, sessionStats, isVictory, false);
            if (isVictory && sessionStats.ContreAttaqueJouee)
            {
                profile.cumuls.victoiresAvecContreAttaque++;
                targetBlock.cumuls.victoiresAvecContreAttaque++;
            }

            var newlyUnlocked = AchievementDefinition.CheckNewlyUnlocked(profile);
            foreach (var id in newlyUnlocked)
            {
                if (!profile.succesDebloques.Contains(id))
                    profile.succesDebloques.Add(id);
            }

            SaveProfile(profile);
        }

        /// <summary>Enregistre une partie abandonnée (ghost).</summary>
        public static void OnGameAbandoned(SessionStats sessionStats)
        {
            if (sessionStats == null) return;
            var profile = LoadProfile();
            if (profile == null)
                profile = PlayerProfile.CreateNew("Joueur");
            if (profile.succesDebloques == null) profile.succesDebloques = new List<string>();
            EnsureStatsBlocks(profile);

            MergeSessionIntoBlock(profile.parties, profile.records, profile.cumuls, profile.cartes, sessionStats, false, true);
            MergeSessionIntoBlock(profile.ghost.parties, profile.ghost.records, profile.ghost.cumuls, profile.ghost.cartes, sessionStats, false, true);

            var newlyUnlocked = AchievementDefinition.CheckNewlyUnlocked(profile);
            foreach (var id in newlyUnlocked)
            {
                if (!profile.succesDebloques.Contains(id))
                    profile.succesDebloques.Add(id);
            }

            SaveProfile(profile);
        }

        private static void EnsureStatsBlocks(PlayerProfile profile)
        {
            if (profile.solo == null) profile.solo = new StatsBlock();
            if (profile.multi == null) profile.multi = new StatsBlock();
            if (profile.ghost == null) profile.ghost = new StatsBlock();
        }

        private static void MergeSessionIntoBlock(PartiesData parties, RecordsData records, CumulsData cumuls, List<CardCount> cartes, SessionStats s, bool isVictory, bool isAbandoned)
        {
            if (parties == null) return;
            if (isAbandoned) { parties.abandonnees++; parties.total++; }
            else { if (isVictory) parties.gagnees++; else parties.perdues++; parties.total++; }
            MergeDeckStats(parties, s, isVictory);
            MergeCartes(cartes ??= new List<CardCount>(), s);
            MergeRecords(records, s);
            MergeCumuls(cumuls, s);
        }

        private static void MergeDeckStats(PartiesData parties, SessionStats s, bool isVictory)
        {
            if (parties.parDeck == null) parties.parDeck = new List<DeckStatsEntry>();
            string deck1 = s.DeckJoueur1 ?? "Magicien";
            var idx = parties.parDeck.FindIndex(x => x.deckName == deck1);
            DeckStatsEntry entry;
            if (idx < 0)
            {
                entry = new DeckStatsEntry { deckName = deck1, jouees = 0, gagnees = 0 };
                parties.parDeck.Add(entry);
            }
            else
            {
                entry = parties.parDeck[idx];
            }
            entry.jouees++;
            if (isVictory) entry.gagnees++;
        }

        private static void MergeCartes(List<CardCount> cartes, SessionStats s)
        {
            foreach (var kv in s.CartesJouees)
            {
                var idx = cartes.FindIndex(x => x.cardId == kv.Key);
                if (idx < 0)
                    cartes.Add(new CardCount { cardId = kv.Key, count = kv.Value });
                else
                    cartes[idx].count += kv.Value;
            }
        }

        private static void MergeRecords(RecordsData records, SessionStats s)
        {
            if (records == null) return;
            if (s.DegatsCeTour > records.maxDegatsUnTour)
                records.maxDegatsUnTour = s.DegatsCeTour;
            if (s.BouclierCeTour > records.maxBouclierUnTour)
                records.maxBouclierUnTour = s.BouclierCeTour;
            if (s.MaxBouclierUnCoup > records.maxBouclierGagneUnCoup)
                records.maxBouclierGagneUnCoup = s.MaxBouclierUnCoup;

            int turns = s.TurnCount;
            if (turns > 0)
            {
                if (turns > records.partieLaPlusLongue)
                {
                    records.partieLaPlusLongue = turns;
                    records.partieRecordTours = new PartieRecordTours
                    {
                        tours = turns,
                        date = DateTime.UtcNow.ToString("O"),
                        deckJoueur1 = s.DeckJoueur1 ?? "",
                        deckJoueur2 = s.DeckJoueur2 ?? "",
                        gagnant = s.Gagnant ?? "",
                        resultatJoueur = s.WinnerIndex == 0 ? "gagné" : "perdu"
                    };
                }
                if (turns > 0 && (records.partieLaPlusCourte == 0 || turns < records.partieLaPlusCourte))
                    records.partieLaPlusCourte = turns;
            }
        }

        private static void MergeCumuls(CumulsData cumuls, SessionStats s)
        {
            if (cumuls == null) return;
            cumuls.degatsInfliges += s.DegatsInfligesTotal;
            cumuls.bouclierGagne += s.BouclierGagneTotal;
            cumuls.cartesPiochees += s.CartesPiocheesTotal;
        }
    }
}
