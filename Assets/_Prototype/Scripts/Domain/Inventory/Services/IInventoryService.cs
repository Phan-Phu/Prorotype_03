using Cysharp.Threading.Tasks;

namespace Prototype.Domain
{
    /// <summary>
    /// Domain port for inventory operations. Infrastructure implements the behavior;
    /// Domain only defines the contract over raw Inventory state. UniTask is part of
    /// the port so callers use one asynchronous API instead of sync/Async duplicates.
    /// </summary>
    public interface IInventoryService
    {
        UniTask<Result<InventoryFailure, Unit>> Add(Inventory inventory, string itemId, int amount = 1);
        UniTask<Result<InventoryFailure, Unit>> Remove(Inventory inventory, string itemId, int amount = 1);
        UniTask<Result<InventoryFailure, Unit>> CanAdd(Inventory inventory, string itemId);
        UniTask<Result<InventoryFailure, int>> Count(Inventory inventory, string itemId);
        UniTask<Result<InventoryFailure, Unit>> Clear(Inventory inventory);
        UniTask<Result<InventoryFailure, Unit>> Swap(Inventory inventory, int firstSlot, int secondSlot);
    }
}
