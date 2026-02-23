using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène Lobby (Phase 2).
    /// Create/Join game (code ami) puis choose deck + confirm.
    /// Pour l'instant : stub avec bouton Retour au menu. Le flux réseau sera ajouté avec Netcode + Relay.
    /// </summary>
    public class LobbyController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button _buttonBackToMenu;

        private void Start()
        {
            if (_buttonBackToMenu != null) _buttonBackToMenu.onClick.AddListener(OnBackToMenu);
        }

        private void OnBackToMenu()
        {
            SceneManager.LoadScene(MenuController.SceneNames.Menu);
        }
    }
}
