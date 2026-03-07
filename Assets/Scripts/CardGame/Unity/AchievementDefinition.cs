using System;
using System.Collections.Generic;

namespace CardGame.Unity
{
    /// <summary>
    /// Définition d'un succès (achievement). La condition est évaluée sur le profil après fusion des stats.
    /// </summary>
    public class AchievementDefinition
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public Func<PlayerProfile, bool> Condition { get; set; }

        /// <summary>Liste de tous les succès du jeu.</summary>
        public static IReadOnlyList<AchievementDefinition> All { get; } = new List<AchievementDefinition>
        {
            new AchievementDefinition
            {
                Id = "premiere_victoire",
                Nom = "Première victoire",
                Description = "Gagnez votre première partie",
                Condition = p => p.parties.gagnees >= 1
            },
            new AchievementDefinition
            {
                Id = "mage_10",
                Nom = "Mage accompli",
                Description = "Gagnez 10 parties avec le deck Magicien",
                Condition = p => GetDeckGagnees(p, "Magicien") >= 10
            },
            new AchievementDefinition
            {
                Id = "guerrier_10",
                Nom = "Guerrier accompli",
                Description = "Gagnez 10 parties avec le deck Guerrier",
                Condition = p => GetDeckGagnees(p, "Guerrier") >= 10
            },
            new AchievementDefinition
            {
                Id = "100_cartes",
                Nom = "Piocheur",
                Description = "Pochez 100 cartes au total",
                Condition = p => p.cumuls.cartesPiochees >= 100
            },
            new AchievementDefinition
            {
                Id = "degats_50",
                Nom = "Dévastateur",
                Description = "Infligez 50 dégâts en un seul tour",
                Condition = p => p.records.maxDegatsUnTour >= 50
            },
            new AchievementDefinition
            {
                Id = "bouclier_30",
                Nom = "Bouclier impénétrable",
                Description = "Gagnez 30 points de bouclier en un tour",
                Condition = p => p.records.maxBouclierUnTour >= 30
            },
            new AchievementDefinition
            {
                Id = "partie_rapide",
                Nom = "Éclair",
                Description = "Terminez une partie en 5 tours ou moins",
                Condition = p => p.records.partieLaPlusCourte > 0 && p.records.partieLaPlusCourte <= 5
            },
            new AchievementDefinition
            {
                Id = "marathon",
                Nom = "Marathonien",
                Description = "Jouez une partie de 30 tours ou plus",
                Condition = p => p.records.partieLaPlusLongue >= 30
            },
            new AchievementDefinition
            {
                Id = "collectionneur",
                Nom = "Collectionneur",
                Description = "Jouez 20 types de cartes différents",
                Condition = p => p.cartes.Count >= 20
            },
            new AchievementDefinition
            {
                Id = "contre_maitre",
                Nom = "Maître de la contre-attaque",
                Description = "Gagnez 5 parties en ayant joué Contre-attaque",
                Condition = p => p.cumuls.victoiresAvecContreAttaque >= 5
            }
        };

        private static int GetDeckGagnees(PlayerProfile p, string deckName)
        {
            var entry = p.parties.parDeck.Find(x => string.Equals(x.deckName, deckName, StringComparison.OrdinalIgnoreCase));
            return entry?.gagnees ?? 0;
        }

        /// <summary>Vérifie tous les succès et retourne les IDs nouvellement débloqués.</summary>
        public static List<string> CheckNewlyUnlocked(PlayerProfile profile)
        {
            var newlyUnlocked = new List<string>();
            if (profile?.succesDebloques == null) return newlyUnlocked;

            foreach (var def in All)
            {
                if (profile.succesDebloques.Contains(def.Id)) continue;
                if (def.Condition != null && def.Condition(profile))
                {
                    newlyUnlocked.Add(def.Id);
                }
            }
            return newlyUnlocked;
        }

        /// <summary>Retourne la définition par ID, ou null.</summary>
        public static AchievementDefinition GetById(string id)
        {
            foreach (var def in All)
            {
                if (def.Id == id) return def;
            }
            return null;
        }

        /// <summary>Retourne une chaîne de progression pour l'affichage UI (ex. "8/10").</summary>
        public static string GetProgressString(PlayerProfile profile, AchievementDefinition def)
        {
            if (profile == null || def == null) return "";
            return def.Id switch
            {
                "premiere_victoire" => $"{profile.parties.gagnees}/1",
                "mage_10" => $"{GetDeckGagnees(profile, "Magicien")}/10",
                "guerrier_10" => $"{GetDeckGagnees(profile, "Guerrier")}/10",
                "100_cartes" => $"{profile.cumuls.cartesPiochees}/100",
                "degats_50" => $"{profile.records.maxDegatsUnTour}/50",
                "bouclier_30" => $"{profile.records.maxBouclierUnTour}/30",
                "partie_rapide" => profile.records.partieLaPlusCourte > 0 ? $"{profile.records.partieLaPlusCourte} tours (≤5)" : "—",
                "marathon" => $"{profile.records.partieLaPlusLongue}/30 tours",
                "collectionneur" => $"{profile.cartes?.Count ?? 0}/20",
                "contre_maitre" => $"{profile.cumuls.victoiresAvecContreAttaque}/5",
                _ => ""
            };
        }
    }
}
