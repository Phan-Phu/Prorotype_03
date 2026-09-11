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
        UniTask<OperationResult> Add(Inventory inventory, string itemId, int amount = 1);
        UniTask<OperationResult> Remove(Inventory inventory, string itemId, int amount = 1);
        UniTask<OperationResult> CanAdd(Inventory inventory, string itemId);
        UniTask<OperationResult<int>> Count(Inventory inventory, string itemId);
        UniTask<OperationResult> Clear(Inventory inventory);
        UniTask<OperationResult> Swap(Inventory inventory, int firstSlot, int secondSlot);
    }
}
