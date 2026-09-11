namespace Prototype.Domain
{
    /// <summary>Typed failure payload for state persistence operations.</summary>
    public sealed class RepositoryFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        RepositoryFailure(FailureCode code, string message, string context = null)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = 0;
            Actual = 0;
        }

        public static RepositoryFailure InvalidArgument(string context)
            => new RepositoryFailure(FailureCode.InvalidArgument, "Repository argument is invalid.", context);

        public static RepositoryFailure System(string context)
            => new RepositoryFailure(FailureCode.SystemError, "Repository system error.", context);
    }
}
