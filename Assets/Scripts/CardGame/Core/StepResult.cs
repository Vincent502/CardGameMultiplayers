namespace CardGame.Core
{
    /// <summary>
    /// Résultat d'un Step() du moteur : avancement automatique ou attente d'action.
    /// </summary>
    public enum StepResult
    {
        PhaseAdvanced,
        NeedPlayAction,
        NeedReaction,
        GameOver
    }
}
