using Cysharp.Threading.Tasks;

namespace Prototype.Domain
{
    /// <summary>
    /// Domain port for inventory operations. Infrastructure implements the behavior;
    /// Domain only defines the contract over raw Inventory state.
    /// </summary>
    public interface IInventoryService
    {
        bool Add(Inventory inventory, string itemId, int amount = 1);
        UniTask<bool> AddAsync(Inventory inventory, string itemId, int amount = 1);
        bool Remove(Inventory inventory, string itemId, int amount = 1);
        UniTask<bool> RemoveAsync(Inventory inventory, string itemId, int amount = 1);
        bool CanAdd(Inventory inventory, string itemId);
        UniTask<bool> CanAddAsync(Inventory inventory, string itemId);
        int Count(Inventory inventory, string itemId);
        UniTask<int> CountAsync(Inventory inventory, string itemId);
        void Clear(Inventory inventory);
        UniTask ClearAsync(Inventory inventory);
        void Swap(Inventory inventory, int firstSlot, int secondSlot);
        UniTask SwapAsync(Inventory inventory, int firstSlot, int secondSlot);
    }
}
