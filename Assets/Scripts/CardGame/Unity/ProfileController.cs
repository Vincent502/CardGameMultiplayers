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
        [SerializeField] private Transform _statsScrollContent;
        [SerializeField] private GameObject _statsItemPrefab;
        [SerializeField] private Transform _succesListContent;
        [SerializeField] private GameObject _succesItemPrefab;
        [SerializeField] private Button _buttonBackFromProfil;
        [Header("Boutons stats (Global, Solo, Multi, Ghost)")]
        [SerializeField] private Button _buttonStatsGlobal;
        [SerializeField] private Button _buttonStatsSolo;
        [SerializeField] private Button _buttonStatsMulti;
        [SerializeField] private Button _buttonStatsGhost;

        private StatsBlockKind _currentStatsBlock = StatsBlockKind.Global;

        private void Start()
        {
            if (_buttonBackFromProfil != null)
                _buttonBackFromProfil.onClick.AddListener(HideProfil);
            if (_buttonStatsGlobal != null) _buttonStatsGlobal.onClick.AddListener(() => SetStatsBlock(StatsBlockKind.Global));
            if (_buttonStatsSolo != null) _buttonStatsSolo.onClick.AddListener(() => SetStatsBlock(StatsBlockKind.Solo));
            if (_buttonStatsMulti != null) _buttonStatsMulti.onClick.AddListener(() => SetStatsBlock(StatsBlockKind.MultiPlayer));
            if (_buttonStatsGhost != null) _buttonStatsGhost.onClick.AddListener(() => SetStatsBlock(StatsBlockKind.Ghost));
        }

        /// <summary>Affiche le panel Profil et charge les données. Global affiché par défaut.</summary>
        public void ShowProfil()
        {
            if (_panelProfil != null)
                _panelProfil.SetActive(true);
            _currentStatsBlock = StatsBlockKind.Global;
            RefreshContent();
        }

        /// <summary>Cache le panel Profil.</summary>
        public void HideProfil()
        {
            if (_panelProfil != null)
                _panelProfil.SetActive(false);
        }

        private void SetStatsBlock(StatsBlockKind kind)
        {
            _currentStatsBlock = kind;
            RefreshContent();
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
            if (_statsScrollContent == null || _statsItemPrefab == null) return;

            foreach (Transform child in _statsScrollContent)
                Destroy(child.gameObject);

            var block = profile.GetStatsBlock(_currentStatsBlock);
            var parties = block.parties ?? new PartiesData();
            var records = block.records ?? new RecordsData();
            var cumuls = block.cumuls ?? new CumulsData();
            var cartes = block.cartes ?? new List<CardCount>();

            string blockLabel = _currentStatsBlock switch
            {
                StatsBlockKind.Global => "Global",
                StatsBlockKind.Solo => "Solo",
                StatsBlockKind.MultiPlayer => "Multi",
                StatsBlockKind.Ghost => "Fantôme",
                _ => "Global"
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<size=120%><b><color={ColorTitle}>{profile.nom}</color></b></size>");
            sb.AppendLine($"<size=90%><color={ColorNeutral}>Stats : {blockLabel}</color></size>");
            sb.AppendLine();

            if (_currentStatsBlock == StatsBlockKind.Ghost)
            {
                sb.AppendLine($"<b><color={ColorSection}>Abandons multi</color></b>");
                sb.AppendLine($"  Parties abandonnées / déconnexion : {parties.abandonnees}");
                sb.AppendLine();
                if (cartes.Count > 0)
                {
                    sb.AppendLine($"<b><color={ColorSection}>Cartes jouées dans les abandons</color></b>");
                    var sorted = new List<CardCount>(cartes);
                    sorted.Sort((a, b) => (b?.count ?? 0).CompareTo(a?.count ?? 0));
                    int toShow = Mathf.Min(10, sorted.Count);
                    for (int i = 0; i < toShow; i++)
                        sb.AppendLine($"  {i + 1}. {sorted[i].cardId} : {sorted[i].count}");
                }
            }
            else
            {
                sb.AppendLine($"<b><color={ColorSection}>Parties</color></b>");
                sb.AppendLine($"  Total : {parties.total}  •  Gagnées : <color={ColorUnlocked}>{parties.gagnees}</color>  •  Perdues : {parties.perdues}  •  Abandonnées : {parties.abandonnees}");
                sb.AppendLine();
                if (parties.parDeck != null && parties.parDeck.Count > 0)
                {
                    sb.AppendLine($"<b><color={ColorSection}>Par deck</color></b>");
                    foreach (var d in parties.parDeck)
                        sb.AppendLine($"  {d.deckName} : {d.jouees} jouées, {d.gagnees} gagnées");
                    sb.AppendLine();
                }
                sb.AppendLine($"<b><color={ColorSection}>Records</color></b>");
                sb.AppendLine($"  Max dégâts en un tour : {records.maxDegatsUnTour}");
                sb.AppendLine($"  Max bouclier en un tour : {records.maxBouclierUnTour}");
                sb.AppendLine($"  Max bouclier en un coup : {records.maxBouclierGagneUnCoup}");
                sb.AppendLine($"  Partie la plus longue : {records.partieLaPlusLongue} tours");
                if (records.partieLaPlusCourte > 0)
                    sb.AppendLine($"  Partie la plus courte : {records.partieLaPlusCourte} tours");
                sb.AppendLine();
                sb.AppendLine($"<b><color={ColorSection}>Cumuls</color></b>");
                sb.AppendLine($"  Dégâts infligés : {cumuls.degatsInfliges}  •  Bouclier gagné : {cumuls.bouclierGagne}  •  Cartes piochées : {cumuls.cartesPiochees}");
                sb.AppendLine();
                if (cartes.Count > 0)
                {
                    sb.AppendLine($"<b><color={ColorSection}>Cartes les plus jouées</color></b>");
                    var sorted = new List<CardCount>(cartes);
                    sorted.Sort((a, b) => (b?.count ?? 0).CompareTo(a?.count ?? 0));
                    int toShow = Mathf.Min(10, sorted.Count);
                    for (int i = 0; i < toShow; i++)
                        sb.AppendLine($"  {i + 1}. {sorted[i].cardId} : {sorted[i].count}");
                }
            }

            AddStatsItem(sb.ToString());
        }

        private void AddStatsItem(string text)
        {
            var go = Instantiate(_statsItemPrefab, _statsScrollContent);
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = text;
                label.richText = true;
            }
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
        }
    }
}
