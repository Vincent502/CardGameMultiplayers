namespace CardGame.Core
{
    /// <summary>
    /// Dégâts en attente de réaction (Parade/Contre-attaque). Si le défenseur ne joue pas de Rapide, ces dégâts sont appliqués.
    /// </summary>
    public class PendingReactionInfo
    {
        public int TargetIndex { get; set; }
        public int AttackerIndex { get; set; }
        public int BaseDamage { get; set; }
        public int CasterForce { get; set; }
        public string SourceName { get; set; }
        public bool UnfreezeAttacker { get; set; }
        /// <summary>True si l'attaque vient d'une frappe (effets à la frappe à appliquer).</summary>
        public bool IsStrike { get; set; }
    }
}
