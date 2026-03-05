using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la section Historique : liste les parties sauvegardées et affiche le détail.
    /// À brancher sur un panel avec : liste (ScrollView Content), détail (Text), bouton retour.
    /// </summary>
    public class HistoryController : MonoBehaviour
    {
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
                    string result = string.IsNullOrEmpty(s.Winner) || s.Winner == "Partie non terminée"
                        ? s.Winner
                        : $"{s.Winner} gagne";
                    label.text = $"{s.DisplayDate}\n{s.DeckJoueur1} vs {s.DeckJoueur2} — {result} ({s.TurnCount} tours)";
                }
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var summary = s;
                    btn.onClick.AddListener(() => ShowDetail(summary));
                }
            }
        }

        private void ShowDetail(GameReportManager.ReportSummary summary)
        {
            if (_panelDetail != null)
                _panelDetail.SetActive(true);
            if (_textDetail == null) return;

            var report = GameReportManager.LoadFullReport(summary.FilePath);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{summary.DisplayTitle}</b>");
            sb.AppendLine($"Date : {summary.DisplayDate}");
            sb.AppendLine($"Tours : {summary.TurnCount}");
            sb.AppendLine();
            sb.AppendLine("--- Journal de la partie ---");
            foreach (var e in report.Entries)
            {
                string timeShort = e.Time.Length > 19 ? e.Time.Substring(11, 8) : e.Time;
                sb.AppendLine($"[{timeShort}] {e.Event}: {e.Data}");
                sb.AppendLine();
            }
            _textDetail.text = sb.ToString();
            _textDetail.overflowMode = TextOverflowModes.Overflow;
            EnsureScrollContentExpands();
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
