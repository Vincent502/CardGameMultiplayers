using CardGame.Core;
using Unity.Netcode;

namespace CardGame.Unity
{
    public enum NetworkActionType : byte
    {
        PlayCard = 0,
        Strike = 1,
        EndTurn = 2,
        DivinationPutBack = 3,
        PlayRapid = 4,
        NoReaction = 5
    }

    /// <summary>
    /// Message réseau pour une GameAction (lockstep P2P).
    /// </summary>
    public struct NetworkActionMessage : INetworkSerializable
    {
        public byte ActionType;
        public int HandIndex;
        public int DivinationPutBackIndex; // -1 = null

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ActionType);
            serializer.SerializeValue(ref HandIndex);
            serializer.SerializeValue(ref DivinationPutBackIndex);
        }

        public static NetworkActionMessage From(GameAction a)
        {
            var m = new NetworkActionMessage { HandIndex = -1, DivinationPutBackIndex = -1 };
            if (a is PlayCardAction p)
            {
                m.ActionType = (byte)NetworkActionType.PlayCard;
                m.HandIndex = p.HandIndex;
                m.DivinationPutBackIndex = p.DivinationPutBackIndex ?? -1;
            }
            else if (a is StrikeAction)
                m.ActionType = (byte)NetworkActionType.Strike;
            else if (a is EndTurnAction)
                m.ActionType = (byte)NetworkActionType.EndTurn;
            else if (a is DivinationPutBackAction d)
            {
                m.ActionType = (byte)NetworkActionType.DivinationPutBack;
                m.HandIndex = d.PutBackIndex;
            }
            else if (a is PlayRapidAction r)
            {
                m.ActionType = (byte)NetworkActionType.PlayRapid;
                m.HandIndex = r.HandIndex;
            }
            else if (a is NoReactionAction)
                m.ActionType = (byte)NetworkActionType.NoReaction;
            return m;
        }

        public GameAction ToGameAction(int playerIndex)
        {
            switch ((NetworkActionType)ActionType)
            {
                case NetworkActionType.PlayCard:
                    return new PlayCardAction
                    {
                        PlayerIndex = playerIndex,
                        HandIndex = HandIndex,
                        DivinationPutBackIndex = DivinationPutBackIndex < 0 ? null : DivinationPutBackIndex
                    };
                case NetworkActionType.Strike:
                    return new StrikeAction { PlayerIndex = playerIndex };
                case NetworkActionType.EndTurn:
                    return new EndTurnAction { PlayerIndex = playerIndex };
                case NetworkActionType.DivinationPutBack:
                    return new DivinationPutBackAction { PlayerIndex = playerIndex, PutBackIndex = HandIndex };
                case NetworkActionType.PlayRapid:
                    return new PlayRapidAction { PlayerIndex = playerIndex, HandIndex = HandIndex };
                case NetworkActionType.NoReaction:
                    return new NoReactionAction { PlayerIndex = playerIndex };
                default:
                    return null;
            }
        }
    }
}
