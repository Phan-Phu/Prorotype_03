namespace Prototype.Domain
{
    /// <summary>
    /// Domain port for inventory operations. Infrastructure implements the behavior;
    /// Domain only defines the contract over raw Inventory state.
    /// </summary>
    public interface IInventoryService
    {
        bool Add(Inventory inventory, string itemId, int amount = 1);
        bool Remove(Inventory inventory, string itemId, int amount = 1);
        bool CanAdd(Inventory inventory, string itemId);
        int Count(Inventory inventory, string itemId);
        void Clear(Inventory inventory);
        void Swap(Inventory inventory, int firstSlot, int secondSlot);
    }
}
