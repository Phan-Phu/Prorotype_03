namespace Prototype.Domain
{
    /// <summary>Typed failure payload for the NPC dialogue use cases.</summary>
    public sealed class DialogueFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        DialogueFailure(FailureCode code, string message, string context = null,
            int expected = 0, int actual = 0)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = expected;
            Actual = actual;
        }

        public static DialogueFailure NotInitialized(string context = "dialogue")
            => new DialogueFailure(FailureCode.NotInitialized, "Dialogue service is not initialized.", context);

        public static DialogueFailure InvalidNpc(string context = "npc")
            => new DialogueFailure(FailureCode.InvalidArgument, "NPC definition is invalid.", context);

        public static DialogueFailure NoActiveDialogue(string context = "dialogue")
            => new DialogueFailure(FailureCode.InvalidState, "There is no active dialogue.", context);

        public static DialogueFailure System(string context)
            => new DialogueFailure(FailureCode.SystemError, "Dialogue system error.", context);
    }
}
