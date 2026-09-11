namespace Prototype.Domain
{
    /// <summary>Typed failure payload for Master Data import and validation.</summary>
    public sealed class MasterDataFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        MasterDataFailure(FailureCode code, string message, string context)
        {
            Code = code; Message = message; Context = context;
            Expected = 0; Actual = 0;
        }

        public static MasterDataFailure MissingAsset(string context = "master_data")
            => new MasterDataFailure(FailureCode.NotInitialized, "Master Data asset is missing.", context);

        public static MasterDataFailure Invalid(string context)
            => new MasterDataFailure(FailureCode.InvalidArgument, "Master Data is invalid.", context);

        public static MasterDataFailure System(string context)
            => new MasterDataFailure(FailureCode.SystemError, "Master Data import failed.", context);
    }
}
