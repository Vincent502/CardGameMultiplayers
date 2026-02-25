using System.Collections;
using CardGame.Core;
using UnityEngine;
using Unity.Netcode;

namespace CardGame.Unity
{
    /// <summary>
    /// Pilote la partie P2P : même moteur que GameController, mais les actions sont envoyées/réceptionnées via le réseau.
    /// Host = Joueur 1 (index 0), Client = Joueur 2 (index 1). Lecture des paramètres depuis NetworkGameParamsHolder.
    /// </summary>
    public class NetworkGameController : MonoBehaviour, IGameController
    {
        public static NetworkGameController Instance { get; private set; }

        [SerializeField] private bool _writeLogToFile = true;
        [Header("Prefab spawné par le Host pour envoyer/recevoir les actions")]
        [SerializeField] private GameObject _gameNetworkPrefab;

        private GameSession _session;
        private IGameLogger _logger;
        private StepResult _lastStepResult;
        private bool _waitingForHumanAction;
        private GameNetworkBehaviour _gameNetwork;
        private bool _humanIsJoueur1;
        private int _localPlayerIndex;
        // Action reçue du réseau (Host ou Client) en attente d'application dans la boucle de jeu.
        private bool _hasPendingRemoteAction;
        private NetworkActionMessage _pendingRemoteAction;

        public GameState State => _session?.State;
        public int LocalPlayerIndex => _localPlayerIndex;
        public bool IsGameOver => State != null && State.WinnerIndex >= 0;
        public bool IsHumanTurn => State != null && State.CurrentPlayer.IsHuman;
        public bool WaitingForHumanAction => _waitingForHumanAction;
        public bool CanStrike => IsHumanTurn && (_session?.CanStrike() ?? false);

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var p = NetworkGameParamsHolder.Params;
            if (!p.HasValue)
            {
                Debug.LogError("[NetworkGameController] No StartGameParams. Return to menu.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(MenuController.SceneNames.Menu);
                return;
            }
            var pr = p.Value;
            _humanIsJoueur1 = NetworkGameParamsHolder.IsHost;
            _localPlayerIndex = NetworkGameParamsHolder.IsHost ? GameState.Player1Index : GameState.Player2Index;
            NetworkGameParamsHolder.Clear();

            _logger = new GameLogger(_writeLogToFile);
            _session = new GameSession(_logger);
            _session.StartGame(_humanIsJoueur1, pr.FirstPlayerIndex, pr.GetDeckJoueur1(), pr.GetDeckJoueur2(), pr.Seed);

            if (NetworkManager.Singleton.IsHost && _gameNetworkPrefab != null)
            {
                var no = _gameNetworkPrefab.GetComponent<NetworkObject>();
                if (no != null)
                {
                    var go = Instantiate(_gameNetworkPrefab);
                    _gameNetwork = go.GetComponent<GameNetworkBehaviour>();
                    go.GetComponent<NetworkObject>().Spawn();
                }
            }
            // Client : l'objet est spawné par le Host et répliqué, il peut arriver 1-2 frames plus tard
            // else : _gameNetwork sera résolu dans Update() quand l'objet sera présent

            _waitingForHumanAction = false;
            _hasPendingRemoteAction = false;
            StartCoroutine(RunGameLoop());
        }

        private void Update()
        {
            // Client : récupérer la référence au GameNetworkBehaviour une fois répliqué par le Host
            if (_gameNetwork == null && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost)
                _gameNetwork = FindObjectOfType<GameNetworkBehaviour>();
        }

        private IEnumerator RunGameLoop()
        {
            while (!IsGameOver)
            {
                _lastStepResult = _session.Step();

                switch (_lastStepResult)
                {
                    case StepResult.PhaseAdvanced:
                        yield return null;
                        break;
                    case StepResult.NeedPlayAction:
                        if (State.CurrentPlayer.IsHuman)
                        {
                            _waitingForHumanAction = true;
                            yield return new WaitUntil(() => !_waitingForHumanAction || IsGameOver);
                        }
                        else
                        {
                            // On attend qu'une action réseau soit reçue pour le joueur adverse.
                            yield return new WaitUntil(() => _hasPendingRemoteAction || IsGameOver);
                            if (!IsGameOver && _hasPendingRemoteAction)
                            {
                                var action = _pendingRemoteAction.ToGameAction(State.CurrentPlayerIndex);
                                if (action != null)
                                    _session.SubmitAction(action);
                                _hasPendingRemoteAction = false;
                            }
                            while (!IsGameOver && _session.Step() == StepResult.PhaseAdvanced)
                                yield return new WaitForSeconds(0.05f);
                        }
                        break;
                    case StepResult.NeedReaction:
                        yield return null;
                        break;
                    case StepResult.GameOver:
                        yield break;
                }
            }
        }

        /// <summary>Appelé par GameNetworkBehaviour quand on reçoit une action de l'autre joueur.</summary>
        public void ApplyActionFromNetwork(NetworkActionMessage msg)
        {
            // On met en file l'action, elle sera appliquée dans la boucle de jeu.
            _pendingRemoteAction = msg;
            _hasPendingRemoteAction = true;
        }

        public void HumanPlayCard(int handIndex, int? divinationPutBackIndex = null)
        {
            if (!_waitingForHumanAction || !IsHumanTurn) return;
            var a = new PlayCardAction { PlayerIndex = State.CurrentPlayerIndex, HandIndex = handIndex, DivinationPutBackIndex = divinationPutBackIndex };
            if (_session.SubmitAction(a))
            {
                _waitingForHumanAction = false;
                var netMsg = NetworkActionMessage.From(a);
                if (NetworkManager.Singleton.IsHost && _gameNetwork != null)
                    _gameNetwork.SendActionToOtherClient(netMsg);
                else if (!NetworkManager.Singleton.IsHost && _gameNetwork != null)
                    _gameNetwork.ReceiveFromClientServerRpc(netMsg);
            }
        }

        public void HumanStrike()
        {
            if (!_waitingForHumanAction || !IsHumanTurn) return;
            if (_session.SubmitAction(new StrikeAction { PlayerIndex = State.CurrentPlayerIndex }))
            {
                _waitingForHumanAction = false;
                var a = new StrikeAction { PlayerIndex = State.CurrentPlayerIndex };
                var netMsg = NetworkActionMessage.From(a);
                if (NetworkManager.Singleton.IsHost && _gameNetwork != null)
                    _gameNetwork.SendActionToOtherClient(netMsg);
                else if (!NetworkManager.Singleton.IsHost && _gameNetwork != null)
                    _gameNetwork.ReceiveFromClientServerRpc(netMsg);
            }
        }

        public void HumanEndTurn()
        {
            if (!_waitingForHumanAction || !IsHumanTurn) return;
            if (_session.SubmitAction(new EndTurnAction { PlayerIndex = State.CurrentPlayerIndex }))
            {
                _waitingForHumanAction = false;
                var a = new EndTurnAction { PlayerIndex = State.CurrentPlayerIndex };
                var netMsg = NetworkActionMessage.From(a);
                if (NetworkManager.Singleton.IsHost && _gameNetwork != null)
                    _gameNetwork.SendActionToOtherClient(netMsg);
                else if (!NetworkManager.Singleton.IsHost && _gameNetwork != null)
                    _gameNetwork.ReceiveFromClientServerRpc(netMsg);
            }
        }
    }
}
