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
        UniTask<bool> Add(Inventory inventory, string itemId, int amount = 1);
        UniTask<bool> Remove(Inventory inventory, string itemId, int amount = 1);
        UniTask<bool> CanAdd(Inventory inventory, string itemId);
        UniTask<int> Count(Inventory inventory, string itemId);
        UniTask Clear(Inventory inventory);
        UniTask Swap(Inventory inventory, int firstSlot, int secondSlot);
    }
}
