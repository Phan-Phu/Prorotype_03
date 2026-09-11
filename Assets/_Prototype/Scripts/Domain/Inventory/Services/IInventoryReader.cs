namespace Prototype.Domain
{
    /// <summary>Read-only port for inventory projections.</summary>
    public interface IInventoryReader
    {
        int SlotCount { get; }
        ItemStack GetSlot(int index);
    }
}
