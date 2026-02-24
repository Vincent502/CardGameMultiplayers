using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène Lobby (Phase 2).
    /// Create / Join avec code ami (Relay), affichage du code quand Host, puis Retour au menu.
    /// Le choix deck + confirmation sera ajouté à l'étape suivante.
    /// </summary>
    public class LobbyController : MonoBehaviour
    {
        [Header("Relay")]
        [SerializeField] private RelayManager _relayManager;
        [Header("Create")]
        [SerializeField] private Button _buttonCreate;
        [SerializeField] private GameObject _createBusyIndicator;
        [SerializeField] private TMP_Text _textJoinCode;
        [Header("Join")]
        [SerializeField] private TMP_InputField _inputJoinCode;
        [SerializeField] private Button _buttonJoin;
        [SerializeField] private GameObject _joinBusyIndicator;
        [Header("Navigation")]
        [SerializeField] private Button _buttonBackToMenu;

        private bool _isBusy;

        private void Start()
        {
            if (_relayManager == null) _relayManager = FindObjectOfType<RelayManager>();
            if (_buttonCreate != null) _buttonCreate.onClick.AddListener(OnCreate);
            if (_buttonJoin != null) _buttonJoin.onClick.AddListener(OnJoin);
            if (_buttonBackToMenu != null) _buttonBackToMenu.onClick.AddListener(OnBackToMenu);
            if (_textJoinCode != null) _textJoinCode.gameObject.SetActive(false);
            SetBusy(false);
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.Singleton.LocalClientId)
                UpdateStatus("Un joueur a rejoint. (Lobby deck à venir)");
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            if (_buttonCreate != null) _buttonCreate.interactable = !busy;
            if (_buttonJoin != null) _buttonJoin.interactable = !busy;
            if (_createBusyIndicator != null) _createBusyIndicator.SetActive(busy && _buttonCreate != null);
            if (_joinBusyIndicator != null) _joinBusyIndicator.SetActive(busy && _buttonJoin != null);
        }

        private void UpdateStatus(string message)
        {
            if (_textJoinCode != null && !string.IsNullOrEmpty(_textJoinCode.text) && message != null)
            {
                _textJoinCode.text = message;
                _textJoinCode.gameObject.SetActive(true);
            }
        }

        private async void OnCreate()
        {
            if (_isBusy || _relayManager == null) return;
            SetBusy(true);
            if (_textJoinCode != null) _textJoinCode.text = "Création en cours...";
            if (_textJoinCode != null) _textJoinCode.gameObject.SetActive(true);
            try
            {
                var joinCode = await _relayManager.StartHostWithRelayAsync();
                if (!string.IsNullOrEmpty(joinCode))
                {
                    if (_textJoinCode != null) _textJoinCode.text = $"Code à partager : {joinCode}";
                    UpdateStatus($"Code à partager : {joinCode}");
                }
                else
                {
                    if (_textJoinCode != null) _textJoinCode.text = "Échec création.";
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (_textJoinCode != null) _textJoinCode.text = "Erreur : " + e.Message;
            }
            SetBusy(false);
        }

        private async void OnJoin()
        {
            if (_isBusy || _relayManager == null) return;
            var code = _inputJoinCode != null ? _inputJoinCode.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                UpdateStatus("Saisis le code.");
                return;
            }
            SetBusy(true);
            try
            {
                var ok = await _relayManager.StartClientWithRelayAsync(code);
                if (ok)
                {
                    if (_textJoinCode != null) _textJoinCode.text = "Connecté. (Lobby deck à venir)";
                    if (_textJoinCode != null) _textJoinCode.gameObject.SetActive(true);
                }
                else
                {
                    if (_textJoinCode != null) _textJoinCode.text = "Échec connexion ou code invalide.";
                    if (_textJoinCode != null) _textJoinCode.gameObject.SetActive(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (_textJoinCode != null) _textJoinCode.text = "Erreur : " + e.Message;
                if (_textJoinCode != null) _textJoinCode.gameObject.SetActive(true);
            }
            SetBusy(false);
        }

        private void OnBackToMenu()
        {
            if (_relayManager != null) _relayManager.Shutdown();
            SceneManager.LoadScene(MenuController.SceneNames.Menu);
        }
    }
}
