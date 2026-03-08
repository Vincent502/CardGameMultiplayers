using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Panneau affichant le nom et la description d'un équipement.
    /// PC : apparaît au survol, reste en place, disparaît 3s après sortie du texte.
    /// </summary>
    public class EquipmentTooltipPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _textLabel;
        [SerializeField] private GameObject _contentRoot;

        private string _currentName;
        private string _currentDescription;
        private bool _isVisible;
        private Coroutine _hideCoroutine;

        private GameObject _mobileOverlay;
        private RectTransform _canvasRect;
        private RectTransform _rectTransform;
        private Camera _canvasCamera;
        private Vector2 _size;
        private Vector2 _offset;

        /// <summary>Indique si le tooltip est actuellement affiché.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Initialise les références si créé à la volée.</summary>
        public void Init(TMP_Text textLabel, GameObject contentRoot, GameObject mobileOverlay,
            RectTransform canvasRect, Camera canvasCamera, Vector2 size, Vector2 offset)
        {
            _textLabel = textLabel;
            _contentRoot = contentRoot != null ? contentRoot : gameObject;
            _mobileOverlay = mobileOverlay;
            _canvasRect = canvasRect;
            _canvasCamera = canvasCamera;
            _rectTransform = GetComponent<RectTransform>();
            _size = size;
            _offset = offset;
            if (_contentRoot != null) _contentRoot.SetActive(false);
            _isVisible = false;
            if (_mobileOverlay != null)
            {
                var btn = _mobileOverlay.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(Hide);
            }
        }

        private void Awake()
        {
            if (_contentRoot != null) _contentRoot.SetActive(false);
            _isVisible = false;
        }

        /// <summary>Affiche le tooltip à la position écran (souris ou touche). Annule tout masquage en attente.</summary>
        public void Show(string cardName, string cardDescription, Vector2 screenPosition)
        {
            CancelPendingHide();
            _currentName = cardName;
            _currentDescription = cardDescription;
            RefreshText();
            transform.SetAsLastSibling();
            SetPosition(screenPosition);
            if (_contentRoot != null) _contentRoot.SetActive(true);
            if (_mobileOverlay != null) _mobileOverlay.SetActive(true);
            _isVisible = true;
        }

        /// <summary>Programme le masquage après un délai (ex. 3s après sortie du survol).</summary>
        public void ScheduleHide(float delaySeconds)
        {
            CancelPendingHide();
            _hideCoroutine = StartCoroutine(DelayedHide(delaySeconds));
        }

        /// <summary>Annule le masquage programmé.</summary>
        public void CancelPendingHide()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
        }

        private IEnumerator DelayedHide(float delay)
        {
            yield return new WaitForSeconds(delay);
            _hideCoroutine = null;
            Hide();
        }

        private void SetPosition(Vector2 screenPosition)
        {
            if (_rectTransform == null || _canvasRect == null) return;
            var rect = _canvasRect.rect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, _canvasCamera, out var local))
                return;
            var pos = local + _offset;
            var half = _size * 0.5f;
            pos.x = Mathf.Clamp(pos.x, rect.xMin + half.x, rect.xMax - half.x);
            pos.y = Mathf.Clamp(pos.y, rect.yMin + half.y, rect.yMax - half.y);
            _rectTransform.anchoredPosition = pos;
        }

        /// <summary>Masque le tooltip immédiatement.</summary>
        public void Hide()
        {
            CancelPendingHide();
            if (_contentRoot != null) _contentRoot.SetActive(false);
            if (_mobileOverlay != null) _mobileOverlay.SetActive(false);
            _isVisible = false;
        }

        /// <summary>Bascule l'affichage (mobile : 1er clic = afficher, 2ème = masquer).</summary>
        public void Toggle(string cardName, string cardDescription, Vector2 screenPosition)
        {
            if (_isVisible && _currentName == cardName)
            {
                Hide();
            }
            else
            {
                Show(cardName, cardDescription, screenPosition);
            }
        }

        private void RefreshText()
        {
            if (_textLabel != null)
            {
                _textLabel.text = string.IsNullOrEmpty(_currentDescription)
                    ? _currentName
                    : $"<b>{_currentName}</b>\n\n{_currentDescription}";
                _textLabel.richText = true;
            }
        }
    }
}
