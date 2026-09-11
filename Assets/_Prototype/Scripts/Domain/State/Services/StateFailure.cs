namespace Prototype.Domain
{
    /// <summary>Typed failure payload for application state snapshots.</summary>
    public sealed class StateFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        StateFailure(FailureCode code, string message, string context = null)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = 0;
            Actual = 0;
        }

        public static StateFailure NotInitialized(string context = "state")
            => new StateFailure(FailureCode.NotInitialized, "Game state is not initialized.", context);

        public static StateFailure System(string context)
            => new StateFailure(FailureCode.SystemError, "Game state system error.", context);

        public static StateFailure FromClock(ClockFailure failure)
            => new StateFailure(failure.Code, failure.Message, failure.Context);
    }
}
