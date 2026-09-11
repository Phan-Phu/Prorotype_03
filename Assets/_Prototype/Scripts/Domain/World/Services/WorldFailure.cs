namespace Prototype.Domain
{
    /// <summary>Typed failure payload for world/debug operations.</summary>
    public sealed class WorldFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        WorldFailure(FailureCode code, string message, string context = null,
            int expected = 0, int actual = 0)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = expected;
            Actual = actual;
        }

        public static WorldFailure NotInitialized(string context = "world")
            => new WorldFailure(FailureCode.NotInitialized, "World state is not initialized.", context);

        public static WorldFailure InvalidTile(string context)
            => new WorldFailure(FailureCode.InvalidTile, "World tile is invalid for this operation.", context);

        public static WorldFailure InvalidArgument(string context)
            => new WorldFailure(FailureCode.InvalidArgument, "World argument is invalid.", context);

        public static WorldFailure System(string context)
            => new WorldFailure(FailureCode.SystemError, "World system error.", context);
    }
}
