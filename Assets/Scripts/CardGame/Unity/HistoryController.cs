using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la section Historique : liste les parties sauvegardées et affiche le détail.
    /// À brancher sur un panel avec : liste (ScrollView Content), détail (Text), bouton retour.
    /// Utilise les balises riches TextMeshPro pour couleurs et mise en forme.
    /// </summary>
    public class HistoryController : MonoBehaviour
    {
        // Palette de couleurs pour le journal (format hex sans # pour TMP)
        private const string ColorTitle = "#FFD700";      // Or
        private const string ColorSection = "#5DADE2";   // Bleu
        private const string ColorDamage = "#E74C3C";     // Rouge
        private const string ColorShield = "#2ECC71";   // Vert
        private const string ColorBuff = "#9B59B6";      // Violet
        private const string ColorVictory = "#F1C40F";  // Or clair
        private const string ColorCard = "#ECF0F1";      // Gris clair
        private const string ColorTime = "#7F8C8D";      // Gris
        private const string ColorNeutral = "#BDC3C7";   // Argent
        [Header("UI Historique")]
        [SerializeField] private GameObject _panelHistorique;
        [SerializeField] private Transform _listContent;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private TMP_Text _textDetail;
        [SerializeField] private RectTransform _scrollDetailContent;
        [SerializeField] private GameObject _panelDetail;
        [SerializeField] private Button _buttonBackFromHistory;
        [SerializeField] private Button _buttonBackFromDetail;

        private List<GameReportManager.ReportSummary> _summaries = new List<GameReportManager.ReportSummary>();

        private void Start()
        {
            if (_buttonBackFromHistory != null)
                _buttonBackFromHistory.onClick.AddListener(() => HideHistorique());
            if (_buttonBackFromDetail != null)
                _buttonBackFromDetail.onClick.AddListener(() => HideDetail());
            if (_panelDetail != null)
                _panelDetail.SetActive(false);
        }

        /// <summary>Affiche le panel Historique et charge la liste des parties.</summary>
        public void ShowHistorique()
        {
            if (_panelHistorique != null)
                _panelHistorique.SetActive(true);
            RefreshList();
        }

        /// <summary>Cache le panel Historique.</summary>
        public void HideHistorique()
        {
            if (_panelHistorique != null)
                _panelHistorique.SetActive(false);
            HideDetail();
        }

        private void RefreshList()
        {
            _summaries = GameReportManager.GetAllSummaries();
            if (_listContent == null || _itemPrefab == null) return;

            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            foreach (var s in _summaries)
            {
                var go = Instantiate(_itemPrefab, _listContent);
                var label = go.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = FormatHistoriqueItem(s);
                    label.richText = true;
                }
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var summary = s;
                    btn.onClick.AddListener(() => ShowDetail(summary));
                }
            }
        }

        /// <summary>Formate un item de la liste historique avec couleurs et mise en forme.</summary>
        private static string FormatHistoriqueItem(GameReportManager.ReportSummary s)
        {
            bool hasWinner = !string.IsNullOrEmpty(s.Winner) && s.Winner != "Partie non terminée";
            string color1 = hasWinner && s.Winner == "Joueur 1" ? ColorShield : (hasWinner ? ColorDamage : ColorSection);
            string color2 = hasWinner && s.Winner == "Joueur 2" ? ColorShield : (hasWinner ? ColorDamage : ColorSection);
            return $"<color={ColorTime}><size=85%>{s.DisplayDate}</size></color>\n" +
                   $"<color={color1}><b>{s.DeckJoueur1}</b></color> vs <color={color2}><b>{s.DeckJoueur2}</b></color>\n" +
                   $"<color={ColorTime}>{s.TurnCount} tours</color>";
        }

        private void ShowDetail(GameReportManager.ReportSummary summary)
        {
            if (_panelDetail != null)
                _panelDetail.SetActive(true);
            if (_textDetail == null) return;

            var report = GameReportManager.LoadFullReport(summary.FilePath);
            var sb = new System.Text.StringBuilder();

            // En-tête
            sb.AppendLine($"<size=120%><b><color={ColorTitle}>{summary.DisplayTitle}</color></b></size>");
            sb.AppendLine($"<color={ColorNeutral}>Date : {summary.DisplayDate}  •  Tours : {summary.TurnCount}</color>");
            sb.AppendLine();
            // Rappel du code couleur
            sb.AppendLine("<size=90%><b>Code couleur :</b></size>");
            sb.AppendLine($"<color={ColorDamage}>● Rouge</color> Dégâts / Perdant  •  <color={ColorShield}>● Vert</color> Bouclier / Gagnant  •  <color={ColorBuff}>● Violet</color> Buffs  •  <color={ColorVictory}>● Or</color> Victoire");
            sb.AppendLine($"<color={ColorCard}>● Gris clair</color> Cartes/Attaques  •  <color={ColorSection}>● Bleu</color> Tours  •  <color={ColorNeutral}>● Argent</color> Pioche/Infos");
            sb.AppendLine();
            sb.AppendLine($"<color={ColorSection}>——— Journal de la partie ———</color>");
            sb.AppendLine();

            foreach (var turnGroup in report.TurnGroups)
            {
                string header = turnGroup.TurnIndex == 0
                    ? "▶ Début"
                    : $"▶ Tour {turnGroup.TurnIndex} — {turnGroup.Joueur} (tour {turnGroup.TurnNumber})";
                sb.AppendLine($"<size=105%><b><color={ColorSection}>{header}</color></b></size>");
                foreach (var e in turnGroup.Entries)
                {
                    var record = e.ToActivityRecord();
                    string display = record.Detail?.ToDisplayText(e.Event);
                    string content = !string.IsNullOrEmpty(display) ? display : e.Data;
                    string coloredLine = FormatEventLine(e.Event, record.TimeShort, content);
                    sb.AppendLine($"  {coloredLine}");
                }
                sb.AppendLine();
            }
            _textDetail.text = sb.ToString();
            _textDetail.overflowMode = TextOverflowModes.Overflow;
            _textDetail.richText = true;
            EnsureScrollContentExpands();
        }

        /// <summary>Applique une couleur selon le type d'événement.</summary>
        private static string FormatEventLine(string eventType, string timeShort, string content)
        {
            string color = eventType switch
            {
                "DamageApplied" or "DamageBlocked" => ColorDamage,
                "ShieldApplied" or "ShieldBuffExpired" or "ArmurePsychique" => ColorShield,
                "RuneAgressivite" or "Galvanisation" or "PositionOffensive" or "PositionDefensive" or "Concentration" or "LienKarmique" or "AppuisSolide" or "ForceBonusExpired" or "ResistanceBuffExpired" => ColorBuff,
                "Victory" => ColorVictory,
                "PlayCard" or "PlayRapid" or "RapidPlayed" or "StrikeReactionPhase" or "ReactionPhase" or "NoReaction" => ColorCard,
                "GameStart" or "StartTurn" or "EndTurn" or "EndTurnRequested" => ColorSection,
                "Draw" or "DeckReshuffled" or "DivinationPutBack" => ColorNeutral,
                "GlaceLocalisee" or "EquipmentUnfrozen" or "OrageDePoche" => ColorBuff,
                "DisciplineEternel" or "SouffleEternel" or "RuneEndurance" or "PassifMagicien" => ColorBuff,
                _ => ColorNeutral
            };
            return $"<color={ColorTime}>[{timeShort}]</color> <color={color}>{content}</color>";
        }

        private void EnsureScrollContentExpands()
        {
            var content = _scrollDetailContent != null ? _scrollDetailContent : _textDetail.transform.parent as RectTransform;
            if (content == null) return;

            var viewport = content.parent as RectTransform;
            float viewportWidth = viewport != null ? viewport.rect.width : 400f;

            if (viewport != null)
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0.5f, 1f);
                content.sizeDelta = new Vector2(0, content.sizeDelta.y);
                content.anchoredPosition = Vector2.zero;
                var contentLE = content.GetComponent<LayoutElement>();
                if (contentLE == null) contentLE = content.gameObject.AddComponent<LayoutElement>();
                contentLE.preferredWidth = viewportWidth;
            }

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _textDetail.textWrappingMode = TMPro.TextWrappingModes.Normal;
            var textRT = _textDetail.rectTransform;
            textRT.anchorMin = new Vector2(0, 1);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.pivot = new Vector2(0.5f, 1f);
            textRT.anchoredPosition = new Vector2(0, 0);
            textRT.sizeDelta = new Vector2(0, textRT.sizeDelta.y);
            _textDetail.alignment = TextAlignmentOptions.TopLeft;

            var textLE = _textDetail.GetComponent<LayoutElement>();
            if (textLE == null) textLE = _textDetail.gameObject.AddComponent<LayoutElement>();
            textLE.preferredWidth = viewportWidth;
            textLE.flexibleWidth = 1;

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void HideDetail()
        {
            if (_panelDetail != null)
                _panelDetail.SetActive(false);
        }
    }
}
