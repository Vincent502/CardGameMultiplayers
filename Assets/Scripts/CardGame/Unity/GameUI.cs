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
        [SerializeField] private TMP_Text _textTurn; // affichage du nombre de tours
        [SerializeField] private Transform _handContainer;
        [SerializeField] private GameObject _cardButtonPrefab;
        [SerializeField] private Button _buttonStrike;
        [SerializeField] private Button _buttonEndTurn;
        [Header("Équipements")]
        [SerializeField] private Transform _equipmentsPlayer0Container;
        [SerializeField] private Transform _equipmentsPlayer1Container;
        [SerializeField] private GameObject _equipmentLabelPrefab;
        [Header("Effets à durée")]
        [SerializeField] private Transform _effectsPlayer0Container;
        [SerializeField] private Transform _effectsPlayer1Container;
        [SerializeField] private GameObject _effectLabelPrefab;

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
            {
                var p0 = state.Players[0];
                _textPlayer0.text =
                    $"Joueur 0 ({p0.DeckKind})\nPV: {p0.PV} Bouclier: {p0.Shield}\nForce: {p0.Force} Résistance: {p0.Resistance}\nMana: {p0.Mana} Main: {p0.Hand.Count}";
            }
            if (_textPlayer1 != null)
            {
                var p1 = state.Players[1];
                _textPlayer1.text =
                    $"Joueur 1 ({p1.DeckKind})\nPV: {p1.PV} Bouclier: {p1.Shield}\nForce: {p1.Force} Résistance: {p1.Resistance}\nMana: {p1.Mana} Main: {p1.Hand.Count}";
            }

            // Tour actuel = 1 quand le premier joueur joue, 2 quand le second, 3 au tour suivant, etc.
            if (_textTurn != null)
            {
                int tourActuel = state.TurnCount + 1;
                _textTurn.text = $"Tour : {tourActuel}";
            }

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
            RefreshEquipments(state);
            RefreshEffects(state);
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

        private void RefreshEquipments(GameState state)
        {
            RefreshEquipmentsForPlayer(state, 0, _equipmentsPlayer0Container);
            RefreshEquipmentsForPlayer(state, 1, _equipmentsPlayer1Container);
        }

        private void RefreshEquipmentsForPlayer(GameState state, int playerIndex, Transform container)
        {
            if (container == null) return;

            foreach (Transform t in container)
                Destroy(t.gameObject);

            var player = state.Players[playerIndex];
            foreach (var eq in player.Equipments)
            {
                var data = DeckDefinitions.GetCard(eq.Card.Id);
                GameObject go = _equipmentLabelPrefab != null
                    ? Instantiate(_equipmentLabelPrefab, container, false)
                    : CreateDefaultEquipmentLabel(container);

                var label = go.GetComponentInChildren<TMP_Text>() ?? go.GetComponent<TMP_Text>();
                if (label == null) continue;

                label.text = data.Name;
                label.color = eq.IsActive ? Color.green : Color.gray;
            }
        }

        private GameObject CreateDefaultEquipmentLabel(Transform parent)
        {
            var go = new GameObject("EquipLabel", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(160f, 24f);

            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = "";
            txt.alignment = TextAlignmentOptions.Left;

            return go;
        }

        private void RefreshEffects(GameState state)
        {
            RefreshEffectsForPlayer(state, 0, _effectsPlayer0Container);
            RefreshEffectsForPlayer(state, 1, _effectsPlayer1Container);
        }

        private void RefreshEffectsForPlayer(GameState state, int playerIndex, Transform container)
        {
            if (container == null) return;

            foreach (Transform t in container)
                Destroy(t.gameObject);

            // On affiche les effets qui impactent ce joueur (TargetPlayerIndex).
            foreach (var effect in state.ActiveDurationEffects)
            {
                if (effect.TargetPlayerIndex != playerIndex) continue;

                var data = DeckDefinitions.GetCard(effect.CardId);
                GameObject go = _effectLabelPrefab != null
                    ? Instantiate(_effectLabelPrefab, container, false)
                    : CreateDefaultEffectLabel(container);

                var label = go.GetComponentInChildren<TMP_Text>() ?? go.GetComponent<TMP_Text>();
                if (label == null) continue;

                label.text = $"{data.Name} ({effect.TurnsRemaining} tours)";
                // Par défaut, vert si plus d'un tour restant, orange si 1 seul.
                label.color = effect.TurnsRemaining > 1 ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.6f, 0.2f);
            }
        }

        private GameObject CreateDefaultEffectLabel(Transform parent)
        {
            var go = new GameObject("EffectLabel", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(180f, 24f);

            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = "";
            txt.alignment = TextAlignmentOptions.Left;

            return go;
        }
    }
}
