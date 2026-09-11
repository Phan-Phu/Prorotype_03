using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Infrastructure port consumed by Application; Domain remains a raw inventory model.</summary>
    public interface IInventoryService
    {
        InventorySnapshot Read(Inventory inventory);
        UniTask<InventorySnapshot> ReadAsync(Inventory inventory);
        bool Add(Inventory inventory, string itemId, int amount = 1);
        bool Remove(Inventory inventory, string itemId, int amount = 1);
        bool CanAdd(Inventory inventory, string itemId);
        int Count(Inventory inventory, string itemId);
        void Clear(Inventory inventory);
        void Swap(Inventory inventory, int firstSlot, int secondSlot);
    }
}
