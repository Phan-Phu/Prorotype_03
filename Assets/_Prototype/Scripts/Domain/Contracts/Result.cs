using Cysharp.Threading.Tasks;

namespace Prototype.Domain
{
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

    /// <summary>Common contract implemented by feature-specific failure types.</summary>
    public interface IFailure
    {
        FailureCode Code { get; }
        string Message { get; }
        string Context { get; }
        int Expected { get; }
        int Actual { get; }
    }

    /// <summary>Marker payload for operations that do not return a value.</summary>
    public readonly struct Unit
    {
        public static readonly Unit Value = new Unit();
    }

    /// <summary>Generic typed result shared by all Infrastructure use cases.</summary>
    public readonly struct Result<TFailure, TValue>
        where TFailure : IFailure
    {
        public readonly bool IsSuccess;
        public readonly TValue Value;
        public readonly TFailure Failure;

        public Result(bool isSuccess, TValue value, TFailure failure)
        {
            IsSuccess = isSuccess;
            Value = value;
            Failure = failure;
        }
    }

    /// <summary>One entry point for creating synchronous and UniTask results.</summary>
    public static class ResultFactory
    {
        public static Result<TFailure, TValue> Success<TFailure, TValue>(TValue value)
            where TFailure : IFailure
            => new Result<TFailure, TValue>(true, value, default(TFailure));

        public static Result<TFailure, TValue> Failure<TFailure, TValue>(TFailure failure)
            where TFailure : IFailure
            => new Result<TFailure, TValue>(false, default(TValue), failure);

        public static Result<TFailure, TValue> Failure<TFailure, TValue>(TFailure failure, TValue value)
            where TFailure : IFailure
            => new Result<TFailure, TValue>(false, value, failure);

        public static Result<TFailure, Unit> Failure<TFailure>(TFailure failure)
            where TFailure : IFailure
            => new Result<TFailure, Unit>(false, default(Unit), failure);

        public static UniTask<Result<TFailure, TValue>> UniTaskSuccess<TFailure, TValue>(TValue value)
            where TFailure : IFailure
            => UniTask.FromResult(Success<TFailure, TValue>(value));

        public static UniTask<Result<TFailure, TValue>> UniTaskFailure<TFailure, TValue>(TFailure failure)
            where TFailure : IFailure
            => UniTask.FromResult(Failure<TFailure, TValue>(failure));

        public static UniTask<Result<TFailure, Unit>> UniTaskFailure<TFailure>(TFailure failure)
            where TFailure : IFailure
            => UniTask.FromResult(Failure<TFailure, Unit>(failure));
    }
}
