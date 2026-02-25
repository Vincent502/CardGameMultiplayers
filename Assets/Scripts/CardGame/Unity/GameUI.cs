using System.Linq;
using CardGame.Core;
using CardGame.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// UI minimale : PV, mana, main (boutons), Frappe, Fin de tour.
    /// Toujours du point de vue local : 1ère position = Moi, 2ème = Adversaire (Solo ou P2P).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _controllerMono;
        private IGameController _controller;
        [SerializeField] private TMP_Text _textStatus;
        [SerializeField] [FormerlySerializedAs("_textPlayer0")] private TMP_Text _textJoueur1;
        [SerializeField] [FormerlySerializedAs("_textPlayer1")] private TMP_Text _textJoueur2;
        [SerializeField] private TMP_Text _textTurn;
        [SerializeField] private Transform _handContainer;
        [SerializeField] private GameObject _cardButtonPrefab;
        [SerializeField] private Button _buttonStrike;
        [SerializeField] private Button _buttonEndTurn;
        [Header("Fin de partie")]
        [SerializeField] private GameObject _panelGameOver;
        [SerializeField] private TMP_Text _textGameOver;
        [SerializeField] private Button _buttonBackToMenu;
        [Header("Équipements")]
        [SerializeField] [FormerlySerializedAs("_equipmentsPlayer0Container")] private Transform _equipmentsJoueur1Container;
        [SerializeField] [FormerlySerializedAs("_equipmentsPlayer1Container")] private Transform _equipmentsJoueur2Container;
        [SerializeField] private GameObject _equipmentLabelPrefab;
        [Header("Effets à durée")]
        [SerializeField] [FormerlySerializedAs("_effectsPlayer0Container")] private Transform _effectsJoueur1Container;
        [SerializeField] [FormerlySerializedAs("_effectsPlayer1Container")] private Transform _effectsJoueur2Container;
        [SerializeField] private GameObject _effectLabelPrefab;

        private int _lastHandCount = -1;
        private int _lastMana = -1;
        private bool _lastNeedsDivinationChoice;
        private bool _lastNeedsReaction;
        private string _lastHandKey;
        private float _reactionTimeRemaining = -1f;
        private const float ReactionWindowDuration = 3f;

        private void Start()
        {
            _controller = _controllerMono as IGameController ?? _controllerMono?.GetComponent<IGameController>();
            if (_controller == null) _controller = FindObjectOfType<GameController>() ?? (IGameController)FindObjectOfType<NetworkGameController>();
            if (_buttonStrike != null) _buttonStrike.onClick.AddListener(() => _controller?.HumanStrike());
            if (_buttonEndTurn != null) _buttonEndTurn.onClick.AddListener(() => _controller?.HumanEndTurn());
            if (_buttonBackToMenu != null) _buttonBackToMenu.onClick.AddListener(OnBackToMenu);
            if (_panelGameOver != null) _panelGameOver.SetActive(false);
        }

        private void Update()
        {
            if (_controller?.State == null) return;

            var state = _controller.State;
            int localIdx = _controller.LocalPlayerIndex;
            int oppIdx = 1 - localIdx;
            // 1ère position = toujours moi, 2ème = adversaire
            if (_textJoueur1 != null)
            {
                var me = state.Players[localIdx];
                _textJoueur1.text =
                    $"Moi - Joueur {localIdx + 1} ({me.DeckKind})\nPV: {me.PV} Bouclier: {me.Shield}\nForce: {me.Force} Résistance: {me.Resistance}\nMana: {me.Mana} Main: {me.Hand.Count}";
            }
            if (_textJoueur2 != null)
            {
                var adv = state.Players[oppIdx];
                _textJoueur2.text =
                    $"Adversaire - Joueur {oppIdx + 1} ({adv.DeckKind})\nPV: {adv.PV} Bouclier: {adv.Shield}\nForce: {adv.Force} Résistance: {adv.Resistance}\nMana: {adv.Mana} Main: {adv.Hand.Count}";
            }

            // Tour actuel = 1 quand le premier joueur joue, 2 quand le second, 3 au tour suivant, etc.
            if (_textTurn != null)
            {
                int tourActuel = state.TurnCount + 1;
                _textTurn.text = $"Tour : {tourActuel}";
            }

            if (_controller.IsGameOver)
            {
                int winnerNum = state.WinnerIndex + 1;
                bool iWon = state.WinnerIndex == localIdx;
                string msg = iWon ? "Partie terminée.\nVous avez gagné !" : $"Partie terminée.\nJoueur {winnerNum} a gagné.";
                if (_textStatus != null) _textStatus.text = msg;
                if (_textGameOver != null) _textGameOver.text = msg;
                if (_panelGameOver != null) _panelGameOver.SetActive(true);
                if (_handContainer != null) _handContainer.gameObject.SetActive(false);
                return;
            }
            if (NetworkGameController.OpponentDisconnected)
            {
                if (_textStatus != null) _textStatus.text = "Adversaire déconnecté.";
                if (_textGameOver != null) _textGameOver.text = "Adversaire déconnecté.\nRetournez au menu.";
                if (_panelGameOver != null) _panelGameOver.SetActive(true);
                if (_handContainer != null) _handContainer.gameObject.SetActive(false);
                return;
            }

            // Fenêtre d'opportunité : 3 sec pour jouer une carte Rapide (uniquement quand c'est notre tour de réagir)
            if (_controller.NeedsReaction && state.ReactionTargetPlayerIndex == _controller.LocalPlayerIndex)
            {
                if (_reactionTimeRemaining < 0)
                    _reactionTimeRemaining = ReactionWindowDuration;
                _reactionTimeRemaining -= Time.deltaTime;
                int secLeft = Mathf.Max(0, Mathf.CeilToInt(_reactionTimeRemaining));
                if (_textStatus != null)
                    _textStatus.text = secLeft > 0 ? $"Réagissez ! {secLeft} sec pour jouer une carte Rapide" : "Temps écoulé...";
                if (_reactionTimeRemaining <= 0)
                {
                    _controller.HumanNoReaction();
                    _reactionTimeRemaining = -1f;
                }
            }
            else
            {
                _reactionTimeRemaining = -1f;
                if (_textStatus != null)
                    _textStatus.text = _controller.NeedsReaction
                        ? "L'adversaire réfléchit..."
                        : _controller.NeedsDivinationChoice
                            ? "Choisissez une carte à remettre sur le deck."
                            : _controller.IsHumanTurn
                                ? "À vous de jouer."
                                : "Tour de l'adversaire...";
            }

            RefreshHand(state);
            RefreshEquipments(state);
            RefreshEffects(state);
            if (_buttonStrike != null) _buttonStrike.interactable = _controller.CanStrike && !_controller.NeedsDivinationChoice && !_controller.NeedsReaction;
            if (_buttonEndTurn != null) _buttonEndTurn.interactable = _controller.IsHumanTurn && !_controller.NeedsDivinationChoice && !_controller.NeedsReaction;
        }

        private void RefreshHand(GameState state)
        {
            if (_handContainer == null) return;
            if (!_controller.IsHumanTurn && !_controller.NeedsDivinationChoice && !_controller.NeedsReaction) return;
            if (_controller.NeedsReaction && state.ReactionTargetPlayerIndex != _controller.LocalPlayerIndex) return;

            var p = _controller.NeedsReaction ? state.Players[state.ReactionTargetPlayerIndex] : state.CurrentPlayer;
            bool needsDiv = _controller.NeedsDivinationChoice;
            bool needsReaction = _controller.NeedsReaction;
            string handKey = string.Join(",", p.Hand.Select(c => c.InstanceId.ToString()));
            int manaOrReserved = needsReaction ? p.ManaReservedForReaction : p.Mana;
            if (p.Hand.Count == _lastHandCount && manaOrReserved == _lastMana && needsDiv == _lastNeedsDivinationChoice && needsReaction == _lastNeedsReaction && handKey == _lastHandKey) return;
            _lastHandCount = p.Hand.Count;
            _lastMana = manaOrReserved;
            _lastNeedsDivinationChoice = needsDiv;
            _lastNeedsReaction = needsReaction;
            _lastHandKey = handKey;

            foreach (Transform t in _handContainer)
                Destroy(t.gameObject);

            RectTransform handRect = _handContainer as RectTransform ?? _handContainer.GetComponent<RectTransform>();
            Transform parent = handRect != null ? (Transform)handRect : _handContainer;

            for (int i = 0; i < p.Hand.Count; i++)
            {
                int index = i;
                var card = p.Hand[i];
                var data = DeckDefinitions.GetCard(card.Id);
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
                    int cost = data.Type == CardType.Equipe ? 0 : data.Cost;
                    SetCardPrefabTexts(btn.transform, data.Name, data.Description, cost);
                    int manaCost = data.Type == CardType.Equipe ? 0 : data.Cost;
                    bool isRapide = data.Type == CardType.Rapide;
                    bool canPlay = needsReaction
                        ? (isRapide && manaOrReserved >= manaCost)
                        : (!isRapide && p.Mana >= manaCost && (card.Id != CardId.Repositionnement || !p.HasPlayedRepositionnementThisTurn));
                    // En mode Divination : seules les 2 cartes piochées sont cliquables pour choisir laquelle remettre sur le deck. Les autres sont désactivées.
                    int handCount = p.Hand.Count;
                    bool isLastTwo = index >= handCount - 2;
                    int putBackIndex = index == handCount - 2 ? 0 : (index == handCount - 1 ? 1 : -1);
                    bool isDivinationChoice = needsDiv && isLastTwo;
                    btn.interactable = needsDiv ? isDivinationChoice : canPlay;
                    btn.onClick.AddListener(() =>
                    {
                        if (!_controller.WaitingForHumanAction) return;
                        if (isDivinationChoice && putBackIndex >= 0)
                            _controller.HumanDivinationPutBack(putBackIndex);
                        else if (needsReaction && canPlay)
                            _controller.HumanPlayRapid(index);
                        else if (canPlay)
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
            int localIdx = _controller.LocalPlayerIndex;
            int oppIdx = 1 - localIdx;
            RefreshEquipmentsForPlayer(state, localIdx, _equipmentsJoueur1Container);
            RefreshEquipmentsForPlayer(state, oppIdx, _equipmentsJoueur2Container);
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
                if (eq.IsFrozen)
                    label.color = new Color(0.25f, 0.45f, 1f); // Bleu quand gelé (Glace localisée)
                else if (eq.IsActive)
                    label.color = Color.green;
                else
                    label.color = Color.gray;
            }
        }

        /// <summary>Remplit les champs CardName, Description, Mana du prefab carte (recherche par nom d'enfant).</summary>
        private void SetCardPrefabTexts(Transform cardRoot, string cardName, string description, int manaCost)
        {
            var cardNameT = cardRoot.Find("CardName");
            if (cardNameT != null)
            {
                var t = cardNameT.GetComponent<TMP_Text>();
                if (t != null) t.text = cardName;
            }
            var descT = cardRoot.Find("Description");
            if (descT != null)
            {
                var t = descT.GetComponent<TMP_Text>();
                if (t != null) t.text = description ?? "";
            }
            var manaT = cardRoot.Find("Mana");
            if (manaT != null)
            {
                var t = manaT.GetComponent<TMP_Text>();
                if (t != null) t.text = manaCost.ToString();
            }
            // Fallback : un seul label (ancien prefab)
            if (cardNameT == null && descT == null && manaT == null)
            {
                var label = cardRoot.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = string.IsNullOrEmpty(description) ? $"{cardName} ({manaCost})" : $"{cardName} ({manaCost})\n{description}";
            }
        }

        private void OnBackToMenu()
        {
            SoloGameParamsHolder.Clear();
            NetworkGameController.ResetOpponentDisconnected();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                var relay = FindObjectOfType<RelayManager>();
                if (relay != null) relay.Shutdown();
                else NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene(MenuController.SceneNames.Menu);
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
            int localIdx = _controller.LocalPlayerIndex;
            int oppIdx = 1 - localIdx;
            RefreshEffectsForPlayer(state, localIdx, _effectsJoueur1Container);
            RefreshEffectsForPlayer(state, oppIdx, _effectsJoueur2Container);
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
