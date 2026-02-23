namespace CardGame.Core
{
    /// <summary>
    /// Type d'effet à durée : dégât chaque tour, ou buff (bouclier/résistance) qui expire.
    /// </summary>
    public enum DurationEffectKind
    {
        /// <summary>Dégât infligé à la cible avant fin de chaque tour (ex. Orage de poche).</summary>
        DamageEachTurn,
        /// <summary>Bouclier temporaire : à l'expiration on retire Value du bouclier (ex. Armure psychique).</summary>
        ShieldBuff,
        /// <summary>Résistance temporaire : à l'expiration on retire Value de la résistance (ex. Lien karmique).</summary>
        ResistanceBuff
    }

    /// <summary>
    /// Effet actif avec une durée en nombre de tours de jeu (spec : 1 tour = 1 joueur).
    /// </summary>
    public class ActiveDurationEffect
    {
        public CardId CardId { get; set; }
        public DurationEffectKind Kind { get; set; }
        public int CasterPlayerIndex { get; set; }
        public int TargetPlayerIndex { get; set; }
        public int TurnsRemaining { get; set; }
        /// <summary>Valeur selon le type (dégât par tour, ou montant de bouclier/résistance à retirer à l'expiration).</summary>
        public int Value { get; set; }
    }
}
