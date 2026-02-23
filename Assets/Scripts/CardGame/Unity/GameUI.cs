using CardGame.Core;
using CardGame.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// UI minimale : PV, mana, main (boutons), Frappe, Fin de tour.
    /// À attacher à un Canvas. Référence le GameController sur la même scène.
    /// Utilise TextMeshPro (TMP_Text) pour les textes.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private GameController _controller;
        [SerializeField] private TMP_Text _textStatus;
        [SerializeField] private TMP_Text _textPlayer0;
        [SerializeField] private TMP_Text _textPlayer1;
        [SerializeField] private Transform _handContainer;
        [SerializeField] private GameObject _cardButtonPrefab;
        [SerializeField] private Button _buttonStrike;
        [SerializeField] private Button _buttonEndTurn;

        private int _lastHandCount = -1;
        private int _lastMana = -1;

        private void Start()
        {
            if (_controller == null) _controller = FindObjectOfType<GameController>();
            if (_buttonStrike != null) _buttonStrike.onClick.AddListener(() => _controller.HumanStrike());
            if (_buttonEndTurn != null) _buttonEndTurn.onClick.AddListener(() => _controller.HumanEndTurn());
        }

        private void Update()
        {
            if (_controller?.State == null) return;

            var state = _controller.State;
            if (_textPlayer0 != null)
                _textPlayer0.text = $"Joueur 0 (Magicien)\nPV: {state.Players[0].PV} Bouclier: {state.Players[0].Shield}\nMana: {state.Players[0].Mana} Main: {state.Players[0].Hand.Count}";
            if (_textPlayer1 != null)
                _textPlayer1.text = $"Joueur 1 (Guerrier)\nPV: {state.Players[1].PV} Bouclier: {state.Players[1].Shield}\nMana: {state.Players[1].Mana} Main: {state.Players[1].Hand.Count}";

            if (_controller.IsGameOver)
            {
                if (_textStatus != null) _textStatus.text = $"Partie terminée. Gagnant : Joueur {state.WinnerIndex}";
                return;
            }

            if (_textStatus != null)
                _textStatus.text = _controller.IsHumanTurn
                    ? "À vous de jouer."
                    : "Tour du bot...";

            RefreshHand(state);
            if (_buttonStrike != null) _buttonStrike.interactable = _controller.IsHumanTurn;
            if (_buttonEndTurn != null) _buttonEndTurn.interactable = _controller.IsHumanTurn;
        }

        private void RefreshHand(GameState state)
        {
            if (_handContainer == null) return;
            if (!_controller.IsHumanTurn) return;

            var p = state.CurrentPlayer;
            if (p.Hand.Count == _lastHandCount && p.Mana == _lastMana) return;
            _lastHandCount = p.Hand.Count;
            _lastMana = p.Mana;

            foreach (Transform t in _handContainer)
                Destroy(t.gameObject);

            RectTransform handRect = _handContainer as RectTransform ?? _handContainer.GetComponent<RectTransform>();
            Transform parent = handRect != null ? (Transform)handRect : _handContainer;

            for (int i = 0; i < p.Hand.Count; i++)
            {
                int index = i;
                var card = p.Hand[i];
                var data = DeckDefinitions.GetCard(card.Id);
                if (data.Type == CardType.Rapide) continue;
                Button btn = _cardButtonPrefab != null
                    ? Instantiate(_cardButtonPrefab, parent, false).GetComponent<Button>()
                    : CreateDefaultButton(parent);
                if (btn != null)
                {
                    var rt = btn.GetComponent<RectTransform>();
                    // Si aucun prefab n'est fourni, on force une taille/position par défaut.
                    // Si un prefab est fourni, on laisse le prefab + le LayoutGroup décider de la taille.
                    if (rt != null && _cardButtonPrefab == null)
                    {
                        rt.anchorMin = new Vector2(0f, 0.5f);
                        rt.anchorMax = new Vector2(0f, 0.5f);
                        rt.pivot = new Vector2(0f, 0.5f);
                        rt.anchoredPosition = Vector2.zero;
                        rt.localScale = Vector3.one;
                        rt.sizeDelta = new Vector2(120f, 40f);
                    }
                    var label = btn.GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = $"{data.Name} ({data.Cost})";
                    bool canPlay = p.Mana >= data.Cost && (data.Type != CardType.Ephemere || !p.EphemereUsed.Contains(card.Id));
                    btn.interactable = canPlay;
                    btn.onClick.AddListener(() =>
                    {
                        if (_controller.WaitingForHumanAction && canPlay)
                            _controller.HumanPlayCard(index);
                    });
                }
            }

            if (handRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
        }

        private Button CreateDefaultButton(Transform parent)
        {
            var go = new GameObject("CardBtn", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(120f, 40f);
            var btn = go.AddComponent<Button>();
            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(4f, 2f);
            txtRt.offsetMax = new Vector2(-4f, -2f);
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.text = "";
            return btn;
        }
    }
}
