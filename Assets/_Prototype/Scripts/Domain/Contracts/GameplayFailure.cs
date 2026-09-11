namespace Prototype.Domain
{
    /// <summary>Shop and gameplay-specific failure payloads for typed Infrastructure results.</summary>
    public sealed class GameplayFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        GameplayFailure(FailureCode code, string message, string context = null, int expected = 0, int actual = 0)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = expected;
            Actual = actual;
        }

        public static GameplayFailure NotInitialized(string context = "gameplay")
            => new GameplayFailure(FailureCode.NotInitialized, "Gameplay service is not initialized.", context);

        public static GameplayFailure InvalidArgument(string context)
            => new GameplayFailure(FailureCode.InvalidArgument, "Gameplay argument is invalid.", context);

        public static GameplayFailure NotEnoughMoney(string itemId, int required, int actual)
            => new GameplayFailure(FailureCode.NotEnoughMoney, "Not enough money to buy item.", itemId, required, actual);

        public static GameplayFailure InventoryFull(string itemId)
            => new GameplayFailure(FailureCode.InventoryFull, "Inventory cannot receive item.", itemId);

        public static GameplayFailure InsufficientInventory(string itemId, int required, int actual)
            => new GameplayFailure(FailureCode.InsufficientInventory, "Not enough items to sell.", itemId, required, actual);

        public static GameplayFailure FromInventory(InventoryFailure failure, string context)
            => new GameplayFailure(failure.Code, failure.Message, context, failure.Expected, failure.Actual);

        public static GameplayFailure Tool(FailureCode code, string context)
            => new GameplayFailure(code, "Tool action could not be completed.", context);

        public static GameplayFailure System(string context)
            => new GameplayFailure(FailureCode.SystemError, "Gameplay system error.", context);
    }
}
