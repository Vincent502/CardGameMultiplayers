using System.Collections;
using CardGame.Core;
using CardGame.Bot;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Pilote Unity : crée la session, fait avancer le moteur, délègue au bot quand ce n'est pas le tour du joueur.
    /// Compatible PC/Mobile. Brancher l'UI (boutons cartes, frappe, fin de tour) sur RequestPlayAction.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private bool _humanIsPlayer0 = true;
        [SerializeField] private bool _writeLogToFile = true;

        private GameSession _session;
        private IGameLogger _logger;
        private SimpleBot _bot;
        private StepResult _lastStepResult;
        private bool _waitingForHumanAction;

        public GameState State => _session?.State;
        public bool IsGameOver => State != null && State.WinnerIndex >= 0;
        public bool IsHumanTurn => State != null && State.CurrentPlayer.IsHuman;
        public bool WaitingForHumanAction => _waitingForHumanAction;

        private void Start()
        {
            _logger = new GameLogger(_writeLogToFile);
            _session = new GameSession(_logger);
            _bot = new SimpleBot();

            int first = Random.Range(0, 2);
            _session.StartGame(_humanIsPlayer0, first);
            _waitingForHumanAction = false;
            StartCoroutine(RunGameLoop());
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
                            var action = _bot.ChooseAction(State);
                            if (action == null)
                                action = new EndTurnAction { PlayerIndex = State.CurrentPlayerIndex };
                            _session.SubmitAction(action);
                            yield return new WaitForSeconds(0.3f);
                            // Continuer à avancer tant qu'on n'a pas besoin d'une nouvelle action (ex. après EndTurn -> StartTurn -> Draw -> Play)
                            while (!IsGameOver && _session.Step() == StepResult.PhaseAdvanced)
                                yield return new WaitForSeconds(0.1f);
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

        /// <summary>Appelé par l'UI quand le joueur humain joue une carte.</summary>
        public void HumanPlayCard(int handIndex, int? divinationPutBackIndex = null)
        {
            if (!_waitingForHumanAction || !IsHumanTurn) return;
            var a = new PlayCardAction { PlayerIndex = State.CurrentPlayerIndex, HandIndex = handIndex, DivinationPutBackIndex = divinationPutBackIndex };
            if (_session.SubmitAction(a))
                _waitingForHumanAction = false;
        }

        /// <summary>Appelé par l'UI quand le joueur humain déclare une frappe.</summary>
        public void HumanStrike()
        {
            if (!_waitingForHumanAction || !IsHumanTurn) return;
            if (_session.SubmitAction(new StrikeAction { PlayerIndex = State.CurrentPlayerIndex }))
                _waitingForHumanAction = false;
        }

        /// <summary>Appelé par l'UI quand le joueur humain termine son tour.</summary>
        public void HumanEndTurn()
        {
            if (!_waitingForHumanAction || !IsHumanTurn) return;
            if (_session.SubmitAction(new EndTurnAction { PlayerIndex = State.CurrentPlayerIndex }))
                _waitingForHumanAction = false;
        }
    }
}
