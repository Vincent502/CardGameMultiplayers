namespace CardGame.Core
{
    /// <summary>
    /// Une instance de carte en jeu (main, deck, cimetière, etc.).
    /// </summary>
    public class CardInstance
    {
        public CardId Id { get; set; }
        public int InstanceId { get; set; }

        public CardInstance(CardId id, int instanceId)
        {
            Id = id;
            InstanceId = instanceId;
        }
    }
}
