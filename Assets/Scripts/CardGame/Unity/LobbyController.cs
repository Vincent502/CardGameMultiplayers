using System;
using CardGame.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

namespace CardGame.Unity
{
    /// <summary>
    /// Contrôleur de la scène Lobby : Create/Join (Relay), choix deck (Magicien/Guerrier), confirmation, puis lancement partie.
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
        [Header("Lobby state (spawné par le Host)")]
        [SerializeField] private GameObject _lobbyStatePrefab;
        [Header("Choix deck (affiché quand connecté)")]
        [SerializeField] private GameObject _panelDeckSelection;
        [SerializeField] private Button _buttonDeckMagicien;
        [SerializeField] private Button _buttonDeckGuerrier;
        [SerializeField] private Button _buttonConfirmDeck;
        [SerializeField] private TMP_Text _textDeckStatus;
        [Header("Navigation")]
        [SerializeField] private Button _buttonBackToMenu;

        private bool _isBusy;
        private DeckKind _selectedDeck = DeckKind.Magicien;
        private bool _myDeckConfirmed;

        private void Start()
        {
            if (_relayManager == null) _relayManager = FindObjectOfType<RelayManager>();
            if (_buttonCreate != null) _buttonCreate.onClick.AddListener(OnCreate);
            if (_buttonJoin != null) _buttonJoin.onClick.AddListener(OnJoin);
            if (_buttonBackToMenu != null) _buttonBackToMenu.onClick.AddListener(OnBackToMenu);
            if (_buttonDeckMagicien != null) _buttonDeckMagicien.onClick.AddListener(() => { _selectedDeck = DeckKind.Magicien; UpdateDeckSelectionUI(); });
            if (_buttonDeckGuerrier != null) _buttonDeckGuerrier.onClick.AddListener(() => { _selectedDeck = DeckKind.Guerrier; UpdateDeckSelectionUI(); });
            if (_buttonConfirmDeck != null) _buttonConfirmDeck.onClick.AddListener(OnConfirmDeck);
            if (_textJoinCode != null) _textJoinCode.gameObject.SetActive(false);
            if (_panelDeckSelection != null) _panelDeckSelection.SetActive(false);
            SetBusy(false);
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void Update()
        {
            if (_panelDeckSelection == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            if (!_panelDeckSelection.activeSelf)
                EnsureLobbyStateAndShowDeckPanel();
            else
            {
                var state = FindObjectOfType<LobbyNetworkState>();
                if (state != null) UpdateDeckStatus(state);
            }
        }

        private void SpawnLobbyStateIfHost()
        {
            if (!NetworkManager.Singleton.IsHost || _lobbyStatePrefab == null) return;
            if (FindObjectOfType<LobbyNetworkState>() != null) return;
            var no = _lobbyStatePrefab.GetComponent<NetworkObject>();
            if (no != null)
            {
                var go = Instantiate(_lobbyStatePrefab);
                go.GetComponent<NetworkObject>().Spawn();
            }
        }

        private void EnsureLobbyStateAndShowDeckPanel()
        {
            if (NetworkManager.Singleton.IsHost) SpawnLobbyStateIfHost();
            var state = FindObjectOfType<LobbyNetworkState>();
            if (state != null)
            {
                _panelDeckSelection.SetActive(true);
                UpdateDeckStatus(state);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.Singleton.LocalClientId)
                SetStatus("Un joueur a rejoint. Choisis ton deck et confirme.");
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            if (_buttonCreate != null) _buttonCreate.interactable = !busy;
            if (_buttonJoin != null) _buttonJoin.interactable = !busy;
            if (_createBusyIndicator != null) _createBusyIndicator.SetActive(busy && _buttonCreate != null);
            if (_joinBusyIndicator != null) _joinBusyIndicator.SetActive(busy && _buttonJoin != null);
        }

        private void SetStatus(string message)
        {
            if (_textJoinCode != null)
            {
                _textJoinCode.text = message;
                _textJoinCode.gameObject.SetActive(true);
            }
        }

        private void UpdateDeckSelectionUI()
        {
            if (_textDeckStatus != null && !_myDeckConfirmed)
                _textDeckStatus.text = $"Deck choisi : {_selectedDeck}. Clique Confirmer.";
        }

        private void UpdateDeckStatus(LobbyNetworkState state)
        {
            if (_textDeckStatus == null) return;
            int host = state.HostDeckChoice;
            int client = state.ClientDeckChoice;
            string j1 = host == LobbyNetworkState.DeckNotSet ? "?" : ((DeckKind)host).ToString();
            string j2 = client == LobbyNetworkState.DeckNotSet ? "?" : ((DeckKind)client).ToString();
            _textDeckStatus.text = $"Joueur 1 : {j1} | Joueur 2 : {j2}" + (_myDeckConfirmed ? " — En attente de l'autre." : " — Choisis et confirme.");
        }

        private void OnConfirmDeck()
        {
            if (_myDeckConfirmed) return;
            var state = FindObjectOfType<LobbyNetworkState>();
            if (state == null) return;
            if (NetworkManager.Singleton.IsHost)
            {
                state.SetHostDeck(_selectedDeck);
                _myDeckConfirmed = true;
            }
            else
            {
                state.SetClientDeckServerRpc((int)_selectedDeck);
                _myDeckConfirmed = true;
            }
            UpdateDeckSelectionUI();
            if (_buttonConfirmDeck != null) _buttonConfirmDeck.interactable = false;
        }

        private async void OnCreate()
        {
            if (_isBusy || _relayManager == null) return;
            SetBusy(true);
            SetStatus("Création en cours...");
            try
            {
                var joinCode = await _relayManager.StartHostWithRelayAsync();
                if (!string.IsNullOrEmpty(joinCode))
                {
                    SpawnLobbyStateIfHost();
                    SetStatus($"Code à partager : {joinCode}");
                }
                else
                    SetStatus("Échec création.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetStatus("Erreur : " + e.Message);
            }
            SetBusy(false);
        }

        private async void OnJoin()
        {
            if (_isBusy || _relayManager == null) return;
            var code = _inputJoinCode != null ? _inputJoinCode.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Saisis le code.");
                return;
            }
            SetBusy(true);
            try
            {
                var ok = await _relayManager.StartClientWithRelayAsync(code);
                if (ok)
                    SetStatus("Connecté. Choisis ton deck (Joueur 2) et confirme.");
                else
                    SetStatus("Échec connexion ou code invalide.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetStatus("Erreur : " + e.Message);
            }
            SetBusy(false);
        }

        private void OnBackToMenu()
        {
            NetworkGameParamsHolder.Clear();
            if (_relayManager != null) _relayManager.Shutdown();
            SceneManager.LoadScene(MenuController.SceneNames.Menu);
        }
    }
}
