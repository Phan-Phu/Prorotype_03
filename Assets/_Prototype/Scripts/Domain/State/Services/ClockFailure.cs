namespace Prototype.Domain
{
    /// <summary>Typed failure payload for clock/time operations.</summary>
    public sealed class ClockFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        ClockFailure(FailureCode code, string message, string context = null,
            int expected = 0, int actual = 0)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = expected;
            Actual = actual;
        }

        public static ClockFailure NotInitialized(string context = "clock")
            => new ClockFailure(FailureCode.NotInitialized, "Clock state is not initialized.", context);

        public static ClockFailure InvalidDelta(float deltaSeconds)
            => new ClockFailure(FailureCode.InvalidArgument, "Delta time must not be negative.", "delta_seconds", 0, (int)(deltaSeconds * 1000f));

        public static ClockFailure System(string context)
            => new ClockFailure(FailureCode.SystemError, "Clock system error.", context);
    }
}
