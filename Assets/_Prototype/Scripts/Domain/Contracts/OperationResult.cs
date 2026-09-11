namespace Prototype.Domain
{
    /// <summary>Stable machine-readable reason returned for an expected operation failure.</summary>
    public enum FailureCode
    {
        None,
        InvalidArgument,
        NotInitialized,
        ItemNotFound,
        InsufficientInventory,
        InventoryFull,
        NotEnoughMoney,
        InvalidState,
        LockedSlot,
        InvalidTile,
        WrongTool,
        NoStamina,
        NoSeed,
        NotChoppable,
        Blocked,
        SystemError
    }

    /// <summary>Structured failure data. UI translates FailureCode into player-facing text.</summary>
    public readonly struct OperationFailure
    {
        public readonly FailureCode Code;
        public readonly string Context;
        public readonly int Expected;
        public readonly int Actual;

        public OperationFailure(FailureCode code, string context = null, int expected = 0, int actual = 0)
        {
            Code = code;
            Context = context;
            Expected = expected;
            Actual = actual;
        }
    }

    /// <summary>Result for an operation that has no return payload.</summary>
    public readonly struct OperationResult
    {
        public readonly bool IsSuccess;
        public readonly OperationFailure Error;
        public FailureCode FailureCode => Error.Code;

        OperationResult(bool isSuccess, OperationFailure error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static OperationResult Success()
            => new OperationResult(true, new OperationFailure(FailureCode.None));

        public static OperationResult Failed(
            FailureCode code,
            string context = null,
            int expected = 0,
            int actual = 0)
            => new OperationResult(false, new OperationFailure(code, context, expected, actual));
    }

    /// <summary>Result for an operation that returns a payload on success.</summary>
    public readonly struct OperationResult<T>
    {
        public readonly bool IsSuccess;
        public readonly T Data;
        public readonly OperationFailure Error;
        public FailureCode FailureCode => Error.Code;

        OperationResult(bool isSuccess, T data, OperationFailure error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
        }

        public static OperationResult<T> Success(T data)
            => new OperationResult<T>(true, data, new OperationFailure(FailureCode.None));

        public static OperationResult<T> Failed(
            FailureCode code,
            string context = null,
            int expected = 0,
            int actual = 0,
            T data = default(T))
            => new OperationResult<T>(false, data, new OperationFailure(code, context, expected, actual));
    }
}
