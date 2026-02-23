using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène Menu : Solo (SoloBoard), Multiplayer (Lobby), Quitter.
    /// Brancher les 3 boutons dans l'Inspector.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        public static class SceneNames
        {
            public const string Menu = "Menu";
            public const string SoloBoard = "SoloBoard";
            public const string Lobby = "Lobby";
            public const string MultiplayeurBoard = "MultiplayeurBoard";
        }

        [Header("Boutons")]
        [SerializeField] private Button _buttonSolo;
        [SerializeField] private Button _buttonMultiplayer;
        [SerializeField] private Button _buttonQuit;

        private void Start()
        {
            if (_buttonSolo != null) _buttonSolo.onClick.AddListener(OnSolo);
            if (_buttonMultiplayer != null) _buttonMultiplayer.onClick.AddListener(OnMultiplayer);
            if (_buttonQuit != null) _buttonQuit.onClick.AddListener(OnQuit);
        }

        private void OnSolo()
        {
            SceneManager.LoadScene(SceneNames.SoloBoard);
        }

        private void OnMultiplayer()
        {
            SceneManager.LoadScene(SceneNames.Lobby);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
