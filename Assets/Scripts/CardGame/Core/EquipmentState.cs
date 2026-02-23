namespace CardGame.Core
{
    /// <summary>
    /// État d'un équipement sur le board : rounds restants avant activation, gel.
    /// </summary>
    public class EquipmentState
    {
        public CardInstance Card { get; set; }
        /// <summary>0 = actif. Diminué de 1 à chaque tour de jeu.</summary>
        public int RoundsUntilActive { get; set; }
        public bool IsFrozen { get; set; }

        public bool IsActive => RoundsUntilActive <= 0 && !IsFrozen;
    }
}
