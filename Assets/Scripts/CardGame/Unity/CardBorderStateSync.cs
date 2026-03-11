using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity
{
    /// <summary>Synchronise la couleur des bordures de carte avec l'état interactable du bouton.
    /// Quand la carte est non jouable, les bordures prennent la même apparence grisée que le reste.</summary>
    public class CardBorderStateSync : MonoBehaviour
    {
        [Tooltip("Conteneur des bordures (Top, Bottom, Left, Right). Si vide, utilise ce GameObject.")]
        [SerializeField] private Transform _bordersContainer;
        [Tooltip("Références manuelles aux bordures. Si vide, cherche tous les Graphic dans le conteneur.")]
        [SerializeField] private Graphic[] _borders;

        private Button _button;
        private Graphic[] _cachedBorders;
        private bool _lastInteractable = true;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null) _button = GetComponentInParent<Button>();
        }

        private void OnEnable()
        {
            _lastInteractable = !(_button != null && _button.interactable);
            RefreshBorders();
            ApplyState();
        }

        private void LateUpdate()
        {
            if (_button == null) return;
            if (_button.interactable == _lastInteractable) return;
            _lastInteractable = _button.interactable;
            ApplyState();
        }

        private void RefreshBorders()
        {
            if (_borders != null && _borders.Length > 0)
            {
                _cachedBorders = _borders;
                return;
            }
            var container = _bordersContainer != null ? _bordersContainer : transform;
            _cachedBorders = container.GetComponentsInChildren<Graphic>(true);
        }

        private void ApplyState()
        {
            if (_button == null) return;
            if (_cachedBorders == null) RefreshBorders();
            if (_cachedBorders == null) return;
            Color c = _button.interactable ? Color.white : _button.colors.disabledColor;
            var target = _button.targetGraphic;
            foreach (var g in _cachedBorders)
            {
                if (g != null && g != target) g.color = c;
            }
        }
    }
}
