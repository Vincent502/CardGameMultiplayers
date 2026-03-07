using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de l'écran Profil : statistiques et succès.
    /// À brancher sur un panel avec sections Stats, Succès, bouton retour.
    /// </summary>
    public class ProfileController : MonoBehaviour
    {
        private const string ColorTitle = "#FFD700";
        private const string ColorSection = "#5DADE2";
        private const string ColorUnlocked = "#2ECC71";
        private const string ColorLocked = "#95A5A6";
        private const string ColorNeutral = "#BDC3C7";

        [Header("UI Profil")]
        [SerializeField] private GameObject _panelProfil;
        [SerializeField] private TMP_Text _textStats;
        [SerializeField] private Transform _succesListContent;
        [SerializeField] private GameObject _succesItemPrefab;
        [SerializeField] private Button _buttonBackFromProfil;

        private void Start()
        {
            if (_buttonBackFromProfil != null)
                _buttonBackFromProfil.onClick.AddListener(HideProfil);
        }

        /// <summary>Affiche le panel Profil et charge les données.</summary>
        public void ShowProfil()
        {
            if (_panelProfil != null)
                _panelProfil.SetActive(true);
            RefreshContent();
        }

        /// <summary>Cache le panel Profil.</summary>
        public void HideProfil()
        {
            if (_panelProfil != null)
                _panelProfil.SetActive(false);
        }

        private void RefreshContent()
        {
            var profile = ProfileManager.LoadProfile();
            if (profile == null) return;

            RefreshStats(profile);
            RefreshSucces(profile);
        }

        private void RefreshStats(PlayerProfile profile)
        {
            if (_textStats == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<size=120%><b><color={ColorTitle}>{profile.nom}</color></b></size>");
            sb.AppendLine();
            sb.AppendLine($"<b><color={ColorSection}>Parties</color></b>");
            sb.AppendLine($"  Total : {profile.parties.total}  •  Gagnées : <color={ColorUnlocked}>{profile.parties.gagnees}</color>  •  Perdues : {profile.parties.perdues}  •  Abandonnées : {profile.parties.abandonnees}");
            sb.AppendLine();
            // Par deck
            if (profile.parties.parDeck != null && profile.parties.parDeck.Count > 0)
            {
                sb.AppendLine($"<b><color={ColorSection}>Par deck</color></b>");
                foreach (var d in profile.parties.parDeck)
                {
                    sb.AppendLine($"  {d.deckName} : {d.jouees} jouées, {d.gagnees} gagnées");
                }
                sb.AppendLine();
            }
            sb.AppendLine($"<b><color={ColorSection}>Records</color></b>");
            sb.AppendLine($"  Max dégâts en un tour : {profile.records.maxDegatsUnTour}");
            sb.AppendLine($"  Max bouclier en un tour : {profile.records.maxBouclierUnTour}");
            sb.AppendLine($"  Max bouclier en un coup : {profile.records.maxBouclierGagneUnCoup}");
            sb.AppendLine($"  Partie la plus longue : {profile.records.partieLaPlusLongue} tours");
            if (profile.records.partieLaPlusCourte > 0)
                sb.AppendLine($"  Partie la plus courte : {profile.records.partieLaPlusCourte} tours");
            sb.AppendLine();
            sb.AppendLine($"<b><color={ColorSection}>Cumuls</color></b>");
            sb.AppendLine($"  Dégâts infligés : {profile.cumuls.degatsInfliges}  •  Bouclier gagné : {profile.cumuls.bouclierGagne}  •  Cartes piochées : {profile.cumuls.cartesPiochees}");
            sb.AppendLine();
            // Top cartes
            if (profile.cartes != null && profile.cartes.Count > 0)
            {
                sb.AppendLine($"<b><color={ColorSection}>Cartes les plus jouées</color></b>");
                var sorted = new List<CardCount>(profile.cartes);
                sorted.Sort((a, b) => (b?.count ?? 0).CompareTo(a?.count ?? 0));
                int toShow = Mathf.Min(10, sorted.Count);
                for (int i = 0; i < toShow; i++)
                {
                    var c = sorted[i];
                    sb.AppendLine($"  {i + 1}. {c.cardId} : {c.count}");
                }
            }
            _textStats.text = sb.ToString();
            _textStats.richText = true;
        }

        private void RefreshSucces(PlayerProfile profile)
        {
            var unlocked = profile.succesDebloques ?? new List<string>();

            if (_succesListContent != null && _succesItemPrefab != null)
            {
                foreach (Transform child in _succesListContent)
                    Destroy(child.gameObject);

                foreach (var def in AchievementDefinition.All)
                {
                    var go = Instantiate(_succesItemPrefab, _succesListContent);
                    var label = go.GetComponentInChildren<TMP_Text>();
                    if (label != null)
                    {
                        bool isUnlocked = unlocked.Contains(def.Id);
                        string color = isUnlocked ? ColorUnlocked : ColorLocked;
                        string progress = AchievementDefinition.GetProgressString(profile, def);
                        string progressStr = !string.IsNullOrEmpty(progress) ? $" ({progress})" : "";
                        label.text = $"<color={color}>{(isUnlocked ? "✓" : "○")}</color> <b>{def.Nom}</b>\n<size=85%><color={ColorNeutral}>{def.Description}{progressStr}</color></size>";
                        label.richText = true;
                    }
                }
            }
            else if (_textStats != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(_textStats.text);
                sb.AppendLine();
                sb.AppendLine($"<b><color={ColorSection}>Succès</color></b>");
                foreach (var def in AchievementDefinition.All)
                {
                    bool isUnlocked = unlocked.Contains(def.Id);
                    string color = isUnlocked ? ColorUnlocked : ColorLocked;
                    string progress = AchievementDefinition.GetProgressString(profile, def);
                    string progressStr = !string.IsNullOrEmpty(progress) ? $" ({progress})" : "";
                    sb.AppendLine($"<color={color}>{(isUnlocked ? "✓" : "○")}</color> {def.Nom} : {def.Description}{progressStr}");
                }
                _textStats.text = sb.ToString();
            }
        }
    }
}
