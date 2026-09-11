namespace Prototype.Domain
{
    /// <summary>Inventory-specific failure payloads used by Result&lt;InventoryFailure, TValue&gt;.</summary>
    public sealed class InventoryFailure : IFailure
    {
        public FailureCode Code { get; }
        public string Message { get; }
        public string Context { get; }
        public int Expected { get; }
        public int Actual { get; }

        InventoryFailure(FailureCode code, string message, string context = null, int expected = 0, int actual = 0)
        {
            Code = code;
            Message = message;
            Context = context;
            Expected = expected;
            Actual = actual;
        }

        public static InventoryFailure NotInitialized(string context = "inventory")
            => new InventoryFailure(FailureCode.NotInitialized, "Inventory is not initialized.", context);

        public static InventoryFailure InvalidArgument(string context)
            => new InventoryFailure(FailureCode.InvalidArgument, "Inventory argument is invalid.", context);

        public static InventoryFailure ItemQuantityNegative(string itemId, int amount)
            => new InventoryFailure(FailureCode.InvalidArgument, "Item quantity must be greater than zero.", itemId, 1, amount);

        public static InventoryFailure ItemNotFound(string itemId)
            => new InventoryFailure(FailureCode.ItemNotFound, "Item was not found in inventory.", itemId);

        public static InventoryFailure InsufficientQuantity(string itemId, int expected, int actual)
            => new InventoryFailure(FailureCode.InsufficientInventory, "Inventory quantity is insufficient.", itemId, expected, actual);

        public static InventoryFailure Full(string itemId)
            => new InventoryFailure(FailureCode.InventoryFull, "Inventory has no available slot.", itemId);

        public static InventoryFailure Locked(string context = "slot")
            => new InventoryFailure(FailureCode.LockedSlot, "Inventory slot is locked.", context);

        public static InventoryFailure System(string context)
            => new InventoryFailure(FailureCode.SystemError, "Inventory system error.", context);
    }
}
