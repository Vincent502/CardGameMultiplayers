using CardGame.Core;

namespace CardGame.Unity
{
    /// <summary>
    /// Interface commune pour GameController (Solo) et NetworkGameController (P2P), utilisée par GameUI.
    /// </summary>
    public interface IGameController
    {
        GameState State { get; }
        /// <summary>Index du joueur local (0 ou 1). En Solo = 0. En P2P : Host = 0, Client = 1.</summary>
        int LocalPlayerIndex { get; }
        bool IsGameOver { get; }
        bool IsHumanTurn { get; }
        bool WaitingForHumanAction { get; }
        bool CanStrike { get; }
        bool NeedsDivinationChoice { get; }
        void HumanPlayCard(int handIndex, int? divinationPutBackIndex = null);
        void HumanDivinationPutBack(int putBackIndex);
        void HumanStrike();
        void HumanEndTurn();
    }
}
