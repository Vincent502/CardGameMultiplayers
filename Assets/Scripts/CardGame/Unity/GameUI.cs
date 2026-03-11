using System.Linq;
using CardGame.Core;
using CardGame.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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
        [SerializeField] private Transform _opponentHandContainer;
        [SerializeField] private GameObject _cardButtonPrefab;
        [SerializeField] private GameObject _cardBackPrefab;
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
        [Header("Tooltip équipement")]
        [SerializeField] private Vector2 _tooltipSize = new Vector2(320f, 120f);
        [SerializeField] private Vector2 _tooltipOffset = new Vector2(12f, 12f);
        [Header("Effets à durée")]
        [SerializeField] [FormerlySerializedAs("_effectsPlayer0Container")] private Transform _effectsJoueur1Container;
        [SerializeField] [FormerlySerializedAs("_effectsPlayer1Container")] private Transform _effectsJoueur2Container;
        [SerializeField] private GameObject _effectLabelPrefab;

        private int _lastHandCount = -1;
        private int _lastOpponentHandCount = -1;
        private int _lastMana = -1;
        private bool _lastNeedsDivinationChoice;
        private bool _lastNeedsReaction;
        private bool _lastIsHumanTurn = true;
        private bool _lastIsDefenderInReaction;
        private string _lastHandKey;
        private string _lastEquipmentsKey;
        private string _lastEffectsKey;
        private string _localPseudo;
        private string _opponentPseudo;
        private float _reactionTimeRemaining = -1f;
        private const float ReactionWindowDuration = 1f;
        private const float ReactionWindowAfterParadeDuration = 2f;
        private bool _lastWasAfterParade;
        private const float HandCardWidth = 200f;
        private const float HandCardHeight = 300f;
        private int _lastSelectedHandIndex = -1;
        private float _lastSelectedHandTime = -1f;
        private const float MinDelayBeforePlay = 0.25f;

#if UNITY_ANDROID || UNITY_IOS
        private static bool IsTouchDevice => true;
#else
        private static bool IsTouchDevice => false;
#endif

        private void Start()
        {
            _controller = _controllerMono as IGameController ?? _controllerMono?.GetComponent<IGameController>();
            if (_controller == null) _controller = FindFirstObjectByType<GameController>() ?? (IGameController)FindFirstObjectByType<NetworkGameController>();
            _localPseudo = _controller?.LocalPseudo ?? "Joueur";
            _opponentPseudo = _controller?.OpponentPseudo ?? "Adversaire";
            if (_buttonStrike != null) _buttonStrike.onClick.AddListener(() => _controller?.HumanStrike());
            if (_buttonEndTurn != null) _buttonEndTurn.onClick.AddListener(() => _controller?.HumanEndTurn());
            if (_buttonBackToMenu != null) _buttonBackToMenu.onClick.AddListener(OnBackToMenu);
            if (_panelGameOver != null) _panelGameOver.SetActive(false);
            var tooltipPanel = CreateEquipmentTooltipPanel();
            if (tooltipPanel != null)
                EquipmentDescriptionTooltip.SetTooltipPanel(tooltipPanel);
        }

        private void Update()
        {
            if (_controller?.State == null) return;

            _localPseudo = _controller.LocalPseudo;
            _opponentPseudo = _controller.OpponentPseudo;

            var state = _controller.State;
            int localIdx = _controller.LocalPlayerIndex;
            int oppIdx = 1 - localIdx;
            // 1ère position = toujours moi, 2ème = adversaire
            if (_textJoueur1 != null)
            {
                var me = state.Players[localIdx];
                _textJoueur1.text =
                    $"{_localPseudo} ({me.DeckKind})\nPV: {me.PV} Bouclier: {me.Shield}\nForce: {me.Force} Résistance: {me.Resistance}\nMana: {me.Mana} Main: {me.Hand.Count} Cim: {me.Graveyard.Count} Éphém: {me.EphemeralConsumedThisGame}";
            }
            if (_textJoueur2 != null)
            {
                var adv = state.Players[oppIdx];
                _textJoueur2.text =
                    $"{_opponentPseudo} ({adv.DeckKind})\nPV: {adv.PV} Bouclier: {adv.Shield}\nForce: {adv.Force} Résistance: {adv.Resistance}\nMana: {adv.Mana} Main: {adv.Hand.Count} Cim: {adv.Graveyard.Count} Éphém: {adv.EphemeralConsumedThisGame}";
            }

            // Tour actuel = 1 quand le premier joueur joue, 2 quand le second, 3 au tour suivant, etc.
            if (_textTurn != null)
            {
                int tourActuel = state.TurnCount + 1;
                _textTurn.text = $"Tour : {tourActuel}";
            }

            if (_controller.IsGameOver)
            {
                bool iWon = state.WinnerIndex == localIdx;
                string winnerName = iWon ? _localPseudo : _opponentPseudo;
                string msg = iWon ? "Partie terminée.\nVous avez gagné !" : $"Partie terminée.\n{winnerName} a gagné.";
                if (_textStatus != null) _textStatus.text = msg;
                if (_textGameOver != null) _textGameOver.text = msg;
                if (_panelGameOver != null) _panelGameOver.SetActive(true);
                if (_handContainer != null) _handContainer.gameObject.SetActive(false);
                if (_opponentHandContainer != null) _opponentHandContainer.gameObject.SetActive(false);
                return;
            }
            if (NetworkGameController.OpponentDisconnected)
            {
                if (_textStatus != null) _textStatus.text = "Adversaire déconnecté.";
                if (_textGameOver != null) _textGameOver.text = "Adversaire déconnecté.\nRetournez au menu.";
                if (_panelGameOver != null) _panelGameOver.SetActive(true);
                if (_handContainer != null) _handContainer.gameObject.SetActive(false);
                if (_opponentHandContainer != null) _opponentHandContainer.gameObject.SetActive(false);
                return;
            }

            // Fenêtre d'opportunité : 1 sec pour Parade/Esquive, 2 sec pour Contre-attaque après Parade.
            if (_controller.NeedsReaction && state.ReactionTargetPlayerIndex == _controller.LocalPlayerIndex)
            {
                bool afterParade = state.PendingContreAttaqueAttackerIndex.HasValue;
                if (afterParade && !_lastWasAfterParade)
                    _reactionTimeRemaining = ReactionWindowAfterParadeDuration;
                else if (_reactionTimeRemaining < 0)
                    _reactionTimeRemaining = afterParade ? ReactionWindowAfterParadeDuration : ReactionWindowDuration;
                _lastWasAfterParade = afterParade;
                _reactionTimeRemaining -= Time.deltaTime;
                if (_textStatus != null)
                {
                    string msg = _reactionTimeRemaining > 0
                        ? (afterParade ? "Contre-attaque possible ! Jouez ou passez." : "Réagissez ! Jouez une carte Rapide (contour visible).")
                        : "Temps écoulé...";
                    _textStatus.text = msg;
                }
                if (_reactionTimeRemaining <= 0)
                {
                    _controller.HumanNoReaction();
                    _reactionTimeRemaining = -1f;
                }
            }
            else
            {
                _reactionTimeRemaining = -1f;
                _lastWasAfterParade = false;
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
            RefreshOpponentHand(state);
            UpdateRapidCardOutlines(state);
            RefreshEquipments(state);
            RefreshEffects(state);
            if (_buttonStrike != null) _buttonStrike.interactable = _controller.CanStrike && !_controller.NeedsDivinationChoice && !_controller.NeedsReaction;
            if (_buttonEndTurn != null) _buttonEndTurn.interactable = _controller.IsHumanTurn && !_controller.NeedsDivinationChoice && !_controller.NeedsReaction;
        }

        private void RefreshHand(GameState state)
        {
            if (_handContainer == null) return;

            int localIdx = _controller.LocalPlayerIndex;
            var p = state.Players[localIdx];
            bool needsDiv = _controller.NeedsDivinationChoice;
            bool needsReaction = _controller.NeedsReaction;
            bool isDefenderInReaction = needsReaction && state.ReactionTargetPlayerIndex == localIdx;
            string handKey = string.Join(",", p.Hand.Select(c => c.InstanceId.ToString()));
            int manaOrReserved = needsReaction ? p.ManaReservedForReaction : p.Mana;
            bool handChanged = p.Hand.Count != _lastHandCount || handKey != _lastHandKey;

            // Ne pas rafraîchir si on n'est pas en phase jouable ET que la main n'a pas changé.
            // Quand on joue une carte rapide, la main change → on force le rafraîchissement pour que la carte jouée disparaisse
            // et que les autres reprennent l'apparence "non jouable".
            // Invalider le cache quand IsHumanTurn change (ex. passage au tour de l'adversaire) pour désactiver les cartes.
            if (!_controller.IsHumanTurn && !needsDiv && !needsReaction && !handChanged && !_lastIsHumanTurn) return;
            if (p.Hand.Count == _lastHandCount && manaOrReserved == _lastMana && needsDiv == _lastNeedsDivinationChoice && needsReaction == _lastNeedsReaction && _controller.IsHumanTurn == _lastIsHumanTurn && isDefenderInReaction == _lastIsDefenderInReaction && handKey == _lastHandKey) return;
            _lastHandCount = p.Hand.Count;
            _lastMana = manaOrReserved;
            _lastNeedsDivinationChoice = needsDiv;
            _lastNeedsReaction = needsReaction;
            _lastIsHumanTurn = _controller.IsHumanTurn;
            _lastIsDefenderInReaction = isDefenderInReaction;
            _lastHandKey = handKey;
            _lastSelectedHandIndex = -1;
            _lastSelectedHandTime = -1f;

            foreach (Transform t in _handContainer)
                Destroy(t.gameObject);

            RectTransform handRect = _handContainer as RectTransform ?? _handContainer.GetComponent<RectTransform>();
            Transform parent = handRect != null ? (Transform)handRect : _handContainer;

            int n = p.Hand.Count;
            var hlg = handRect != null ? handRect.GetComponent<HorizontalLayoutGroup>() : null;
            if (hlg != null)
            {
                hlg.enabled = true;
                if (n > TwoTapHandThreshold)
                {
                    float containerWidth = handRect.rect.width > 0 ? handRect.rect.width : handRect.sizeDelta.x;
                    if (containerWidth <= 0) containerWidth = 1000f;
                    hlg.spacing = (containerWidth - HandCardWidth) / (n - 1) - HandCardWidth;
                }
                else
                {
                    hlg.spacing = HandSpacingWhenFewCards;
                }
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            for (int i = 0; i < n; i++)
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
                    if (rt != null)
                    {
                        var idxComp = btn.gameObject.GetComponent<HandCardIndex>() ?? btn.gameObject.AddComponent<HandCardIndex>();
                        idxComp.Index = i;
                        if (_cardButtonPrefab == null)
                        {
                            rt.localScale = Vector3.one;
                            rt.sizeDelta = new Vector2(120f, 40f);
                        }
                        else
                        {
                            AddBringToFrontOnHover(btn);
                        }
                    }
                    int cost = data.Type == CardType.Equipe ? 0 : data.Cost;
                    SetCardPrefabTexts(btn.transform, data.Name, data.Description, cost, data.Type);
                    int manaCost = data.Type == CardType.Equipe ? 0 : data.Cost;
                    bool isRapide = data.Type == CardType.Rapide;
                    bool outlineVisible = needsReaction && _reactionTimeRemaining > 0;
                    bool afterParade = state.PendingContreAttaqueAttackerIndex.HasValue;
                    bool rapideAllowed = afterParade
                        ? (card.Id == CardId.ContreAttaque)
                        : (card.Id != CardId.ContreAttaque);
                    bool canPlay = needsReaction
                        ? (isDefenderInReaction && isRapide && rapideAllowed && manaOrReserved >= manaCost && outlineVisible)
                        : (_controller.IsHumanTurn && !isRapide && p.Mana >= manaCost && (card.Id != CardId.Repositionnement || !p.HasPlayedRepositionnementThisTurn));
                    bool isDivinationChoice = needsDiv;
                    btn.interactable = needsDiv ? true : canPlay;
                    float reactionDuration = afterParade ? ReactionWindowAfterParadeDuration : ReactionWindowDuration;
                    ApplyRapidCardOutline(btn, isRapide && needsReaction, outlineVisible ? Mathf.Clamp01(_reactionTimeRemaining / reactionDuration) : 0f);
                    int handCount = p.Hand.Count;
                    btn.onClick.AddListener(() => OnHandCardClicked(index, btn, handCount, isDivinationChoice, needsReaction, canPlay));
                }
            }

            if (handRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
        }

        private const int TwoTapHandThreshold = 5;
        private const float HandSpacingWhenFewCards = 10f;

        private void OnHandCardClicked(int index, Button btn, int handCount, bool isDivinationChoice, bool needsReaction, bool canPlay)
        {
            if (!_controller.WaitingForHumanAction) return;
            bool useTwoTap = IsTouchDevice && handCount > TwoTapHandThreshold;
            if (useTwoTap && (isDivinationChoice || (canPlay && !isDivinationChoice)))
            {
                float now = Time.unscaledTime;
                bool sameCard = index == _lastSelectedHandIndex;
                bool delayOk = (now - _lastSelectedHandTime) >= MinDelayBeforePlay;
                if (sameCard && delayOk)
                {
                    if (isDivinationChoice)
                        _controller.HumanDivinationPutBack(index);
                    else if (needsReaction)
                        _controller.HumanPlayRapid(index);
                    else
                        _controller.HumanPlayCard(index);
                    _lastSelectedHandIndex = -1;
                    _lastSelectedHandTime = -1f;
                }
                else
                {
                    SetCardOnTop(btn);
                    _lastSelectedHandIndex = index;
                    _lastSelectedHandTime = now;
                }
            }
            else if (isDivinationChoice)
            {
                _controller.HumanDivinationPutBack(index);
            }
            else if (canPlay)
            {
                if (needsReaction)
                    _controller.HumanPlayRapid(index);
                else
                    _controller.HumanPlayCard(index);
            }
        }

        private void AddBringToFrontOnHover(Button btn)
        {
            if (IsTouchDevice) return;
            var trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => SetCardOnTop(btn));
            trigger.triggers.Add(enterEntry);
        }

        /// <summary>Met la carte au premier plan (rendu) sans la déplacer, via Canvas.sortingOrder.</summary>
        private void SetCardOnTop(Button btn)
        {
            if (btn == null || _handContainer == null) return;
            foreach (Transform t in _handContainer)
            {
                var c = t.GetComponent<Canvas>();
                if (c != null) c.sortingOrder = 0;
            }
            var canvas = btn.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = btn.gameObject.AddComponent<Canvas>();
                btn.gameObject.AddComponent<GraphicRaycaster>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1;
        }

        /// <summary>Affiche les dos de cartes de l'adversaire. Mis à jour quand sa main change.</summary>
        private void RefreshOpponentHand(GameState state)
        {
            if (_opponentHandContainer == null) return;

            int oppIdx = 1 - _controller.LocalPlayerIndex;
            var opp = state.Players[oppIdx];
            int count = opp.Hand.Count;

            if (count == _lastOpponentHandCount) return;
            _lastOpponentHandCount = count;

            foreach (Transform t in _opponentHandContainer)
                Destroy(t.gameObject);

            _opponentHandContainer.gameObject.SetActive(count > 0);
            if (count == 0) return;

            RectTransform containerRect = _opponentHandContainer as RectTransform ?? _opponentHandContainer.GetComponent<RectTransform>();
            Transform parent = containerRect != null ? (Transform)containerRect : _opponentHandContainer;

            GameObject prefab = _cardBackPrefab != null ? _cardBackPrefab : _cardButtonPrefab;
            for (int i = 0; i < count; i++)
            {
                GameObject go = prefab != null
                    ? Instantiate(prefab, parent, false)
                    : CreateDefaultCardBack(parent);
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
                if (prefab == _cardButtonPrefab)
                    SetCardBackMode(go.transform);
            }

            if (containerRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        /// <summary>Cache les infos de carte et applique l'apparence dos de carte (quand on réutilise le prefab carte).</summary>
        private void SetCardBackMode(Transform cardRoot)
        {
            foreach (var name in new[] { "CardName", "Description", "Mana", "Type" })
            {
                var child = cardRoot.Find(name);
                if (child != null) child.gameObject.SetActive(false);
            }
            var img = cardRoot.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = new Color(0.25f, 0.2f, 0.35f);
        }

        private GameObject CreateDefaultCardBack(Transform parent)
        {
            var go = new GameObject("CardBack", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 120f);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.25f, 0.2f, 0.35f);
            return go;
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
            var key = string.Join("|", state.Players[localIdx].Equipments.Select(e => $"{e.Card.Id}_{e.IsActive}_{e.IsFrozen}"))
                + "||" + string.Join("|", state.Players[oppIdx].Equipments.Select(e => $"{e.Card.Id}_{e.IsActive}_{e.IsFrozen}"));
            if (key == _lastEquipmentsKey) return;
            _lastEquipmentsKey = key;
            RefreshEquipmentsForPlayer(state, localIdx, _equipmentsJoueur1Container);
            RefreshEquipmentsForPlayer(state, oppIdx, _equipmentsJoueur2Container);
        }

        private void RefreshEquipmentsForPlayer(GameState state, int playerIndex, Transform container)
        {
            if (container == null) return;

            EquipmentDescriptionTooltip.HideTooltip();
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

                label.raycastTarget = true;
                var tooltipTarget = label.gameObject;
                var tooltip = tooltipTarget.GetComponent<EquipmentDescriptionTooltip>();
                if (tooltip == null) tooltip = tooltipTarget.AddComponent<EquipmentDescriptionTooltip>();
                tooltip.SetCardData(data);
            }
        }

        private EquipmentTooltipPanel CreateEquipmentTooltipPanel()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;
            var tooltipGO = new GameObject("EquipmentTooltipPanel");
            tooltipGO.transform.SetParent(canvas.transform, false);
            var rt = tooltipGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = _tooltipSize;
            var img = tooltipGO.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            img.raycastTarget = false;
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(tooltipGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(12, 12);
            textRT.offsetMax = new Vector2(-12, -12);
            var txt = textGO.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 16;
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 20;
            txt.fontSizeMax = 30;
            txt.richText = true;
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.enableWordWrapping = true;
            GameObject overlay = null;
            if (Application.isMobilePlatform)
            {
                overlay = new GameObject("TooltipOverlay");
                overlay.transform.SetParent(canvas.transform, false);
                overlay.transform.SetAsLastSibling();
                var overlayRt = overlay.AddComponent<RectTransform>();
                overlayRt.anchorMin = Vector2.zero;
                overlayRt.anchorMax = Vector2.one;
                overlayRt.offsetMin = Vector2.zero;
                overlayRt.offsetMax = Vector2.zero;
                var overlayImg = overlay.AddComponent<Image>();
                overlayImg.color = new Color(0, 0, 0, 0.01f);
                overlayImg.raycastTarget = true;
                var overlayBtn = overlay.AddComponent<Button>();
                overlayBtn.transition = Selectable.Transition.None;
                overlay.SetActive(false);
            }
            var panel = tooltipGO.AddComponent<EquipmentTooltipPanel>();
            var canvasComponent = canvas.GetComponent<Canvas>();
            var canvasRect = canvas.transform as RectTransform;
            var cam = canvasComponent != null && canvasComponent.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvasComponent.worldCamera : null;
            panel.Init(txt, tooltipGO, overlay, canvasRect, cam, _tooltipSize, _tooltipOffset);
            tooltipGO.SetActive(false);
            return panel;
        }

        /// <summary>Remplit les champs CardName, Description, Mana, Type du prefab carte (recherche par nom d'enfant).</summary>
        private void SetCardPrefabTexts(Transform cardRoot, string cardName, string description, int manaCost, CardType cardType)
        {
            string typeLabel = GetCardTypeLabel(cardType);
            var cardNameT = cardRoot.Find("CardName");
            var typeT = cardRoot.Find("Type");
            if (cardNameT != null)
            {
                var t = cardNameT.GetComponent<TMP_Text>();
                if (t != null)
                    t.text = typeT != null ? cardName : $"{cardName} [{typeLabel}]";
            }
            if (typeT != null)
            {
                var t = typeT.GetComponent<TMP_Text>();
                if (t != null) t.text = typeLabel;
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
            if (cardNameT == null && descT == null && manaT == null && typeT == null)
            {
                var label = cardRoot.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = string.IsNullOrEmpty(description)
                        ? $"{cardName} [{typeLabel}] ({manaCost})"
                        : $"{cardName} [{typeLabel}] ({manaCost})\n{description}";
            }
        }

        /// <summary>Met à jour le contour des cartes Rapides existantes (sans reconstruire la main). Permet le fondu sur 1 sec sans bug IA.
        /// En phase réaction : les cartes non-rapides restent non jouables (interactable=false, pas de contour).</summary>
        private void UpdateRapidCardOutlines(GameState state)
        {
            if (!_controller.NeedsReaction || state.ReactionTargetPlayerIndex != _controller.LocalPlayerIndex || _handContainer == null) return;
            bool afterParade = state.PendingContreAttaqueAttackerIndex.HasValue;
            float reactionDuration = afterParade ? ReactionWindowAfterParadeDuration : ReactionWindowDuration;
            float alpha = _reactionTimeRemaining > 0 ? Mathf.Clamp01(_reactionTimeRemaining / reactionDuration) : 0f;
            var p = state.Players[state.ReactionTargetPlayerIndex];
            int manaOrReserved = p.ManaReservedForReaction;
            foreach (Transform t in _handContainer)
            {
                var idxComp = t.GetComponent<HandCardIndex>();
                int index = idxComp != null ? idxComp.Index : -1;
                if (index < 0 || index >= p.Hand.Count) continue;
                var card = p.Hand[index];
                var data = DeckDefinitions.GetCard(card.Id);
                var btn = t.GetComponent<Button>();
                if (btn == null) { index++; continue; }
                if (data.Type == CardType.Rapide)
                {
                    int manaCost = data.Type == CardType.Equipe ? 0 : data.Cost;
                    bool rapideAllowed = afterParade ? (card.Id == CardId.ContreAttaque) : (card.Id != CardId.ContreAttaque);
                    bool canPlayRapid = rapideAllowed && manaOrReserved >= manaCost && alpha > 0.01f;
                    btn.interactable = canPlayRapid;
                    ApplyRapidCardOutline(btn, true, alpha);
                }
                else
                {
                    btn.interactable = false;
                    ApplyRapidCardOutline(btn, false, 0f);
                }
            }
        }

        /// <summary>Applique un contour spécial aux cartes Rapides pendant la fenêtre de réaction. Le contour disparaît en fondu sur 1 sec.
        /// Si isRapidInReaction=false, désactive le contour (pour les cartes non-rapides en phase réaction).</summary>
        private void ApplyRapidCardOutline(Button btn, bool isRapidInReaction, float outlineAlpha)
        {
            if (btn == null) return;
            var img = btn.targetGraphic ?? btn.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = btn.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img == null) return;
            var outline = img.GetComponent<Outline>();
            if (outline == null) outline = img.gameObject.AddComponent<Outline>();
            bool showOutline = isRapidInReaction && outlineAlpha > 0.01f;
            outline.enabled = showOutline;
            outline.effectColor = new Color(1f, 0.9f, 0.2f, outlineAlpha);
            outline.effectDistance = new Vector2(4, 4);
        }

        private static string GetCardTypeLabel(CardType t)
        {
            switch (t)
            {
                case CardType.Equipe: return "Équipé";
                case CardType.Normal: return "Normal";
                case CardType.Ephemere: return "Éphémère";
                case CardType.Rapide: return "Rapide";
                default: return t.ToString();
            }
        }

        private void OnBackToMenu()
        {
            _controller?.NotifyQuitToMenu();
            SoloGameParamsHolder.Clear();
            NetworkGameController.ResetOpponentDisconnected();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                var relay = FindFirstObjectByType<RelayManager>();
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
            string effectsKey = string.Join("|", state.ActiveDurationEffects.Select(e => $"{e.TargetPlayerIndex}_{e.CardId}_{e.TurnsRemaining}"));
            if (effectsKey == _lastEffectsKey) return;
            _lastEffectsKey = effectsKey;

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
