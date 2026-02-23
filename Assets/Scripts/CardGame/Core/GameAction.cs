namespace CardGame.Core
{
    /// <summary>
    /// Action jouée par un joueur (humain ou bot) pendant la phase Play ou Reaction.
    /// </summary>
    public abstract class GameAction
    {
        public int PlayerIndex { get; set; }
    }

    public class PlayCardAction : GameAction
    {
        public int HandIndex { get; set; }
        /// <summary>Pour Divination : index dans la main de la carte à remettre sur le deck (0 ou 1).</summary>
        public int? DivinationPutBackIndex { get; set; }
    }

    public class StrikeAction : GameAction { }

    public class EndTurnAction : GameAction { }

    public class PlayRapidAction : GameAction
    {
        public int HandIndex { get; set; }
    }

    public class NoReactionAction : GameAction { }
}
