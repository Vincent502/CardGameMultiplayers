namespace CardGame.Core
{
    /// <summary>
    /// Interface de logging pour traçabilité et debug (spec PROMPT : tout logger).
    /// </summary>
    public interface IGameLogger
    {
        void Log(string eventType, object data);
        /// <summary>Finalise le rapport quand la partie se termine (pour l'historique).</summary>
        void FinalizeReport(GameState state);
    }
}
