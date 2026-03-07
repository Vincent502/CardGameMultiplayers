using System;
using System.IO;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Gère le profil joueur : existence, création, chargement, sauvegarde.
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
    }
}
