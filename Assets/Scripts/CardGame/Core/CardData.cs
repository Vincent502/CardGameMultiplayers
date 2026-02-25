namespace CardGame.Core
{
    /// <summary>
    /// Données invariantes d'une carte (spec carte_spec_complete.md).
    /// Pour Equipé : Cost = nombre de rounds avant activation.
    /// Pour les autres : Cost = coût en mana.
    /// </summary>
    public readonly struct CardData
    {
        public CardId Id { get; }
        public string Name { get; }
        public CardType Type { get; }
        /// <summary>Mana pour jouer, ou rounds avant activation pour Equipé.</summary>
        public int Cost { get; }
        /// <summary>Description de l'effet (affichée dans l'UI).</summary>
        public string Description { get; }

        public CardData(CardId id, string name, CardType type, int cost, string description = null)
        {
            Id = id;
            Name = name;
            Type = type;
            Cost = cost;
            Description = description ?? "";
        }
    }
}
