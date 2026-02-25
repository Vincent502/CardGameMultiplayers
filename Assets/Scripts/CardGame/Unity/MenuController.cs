using CardGame.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène Menu : Solo (choix deck puis SoloBoard), Multiplayer (Lobby), Quitter.
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

        [Header("Menu principal")]
        [SerializeField] private GameObject _panelMainMenu;
        [Header("Boutons")]
        [SerializeField] private Button _buttonSolo;
        [SerializeField] private Button _buttonMultiplayer;
        [SerializeField] private Button _buttonQuit;
        [Header("Solo - Choix deck")]
        [SerializeField] private GameObject _panelSoloDeckSelection;
        [SerializeField] private Button _buttonDeckMagicien;
        [SerializeField] private Button _buttonDeckGuerrier;
        [SerializeField] private Button _buttonLaunchSolo;
        [SerializeField] private Button _buttonBackToMenuFromSolo;
        [SerializeField] private TMP_Text _textSoloDeckStatus;

        private DeckKind _selectedDeck = DeckKind.Magicien;

        private void Start()
        {
            if (_buttonSolo != null) _buttonSolo.onClick.AddListener(OnSolo);
            if (_buttonMultiplayer != null) _buttonMultiplayer.onClick.AddListener(OnMultiplayer);
            if (_buttonQuit != null) _buttonQuit.onClick.AddListener(OnQuit);
            if (_buttonDeckMagicien != null) _buttonDeckMagicien.onClick.AddListener(() => { _selectedDeck = DeckKind.Magicien; UpdateSoloDeckUI(); });
            if (_buttonDeckGuerrier != null) _buttonDeckGuerrier.onClick.AddListener(() => { _selectedDeck = DeckKind.Guerrier; UpdateSoloDeckUI(); });
            if (_buttonLaunchSolo != null) _buttonLaunchSolo.onClick.AddListener(OnLaunchSolo);
            if (_buttonBackToMenuFromSolo != null) _buttonBackToMenuFromSolo.onClick.AddListener(OnCancelSoloDeck);
            if (_panelSoloDeckSelection != null) _panelSoloDeckSelection.SetActive(false);
        }

        private void OnSolo()
        {
            if (_panelSoloDeckSelection != null)
            {
                if (_panelMainMenu != null) _panelMainMenu.SetActive(false);
                _panelSoloDeckSelection.SetActive(true);
                _selectedDeck = DeckKind.Magicien;
                UpdateSoloDeckUI();
            }
            else
                LaunchSoloWithDeck(DeckKind.Magicien);
        }

        private void UpdateSoloDeckUI()
        {
            if (_textSoloDeckStatus != null)
                _textSoloDeckStatus.text = $"Deck choisi : {_selectedDeck}. L'IA aura un deck aléatoire.";
        }

        private void OnLaunchSolo()
        {
            LaunchSoloWithDeck(_selectedDeck);
        }

        private void OnCancelSoloDeck()
        {
            if (_panelSoloDeckSelection != null)
                _panelSoloDeckSelection.SetActive(false);
            if (_panelMainMenu != null)
                _panelMainMenu.SetActive(true);
        }

        private void LaunchSoloWithDeck(DeckKind humanDeck)
        {
            var botDeck = (DeckKind)UnityEngine.Random.Range(0, 2);
            SoloGameParamsHolder.Set(humanDeck, botDeck);
            if (_panelSoloDeckSelection != null)
                _panelSoloDeckSelection.SetActive(false);
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
