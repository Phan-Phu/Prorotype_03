namespace Prototype.Domain
{
    /// <summary>Read-only domain port used by application queries; prevents UI from mutating inventory.</summary>
    public interface IInventoryReader
    {
        int SlotCount { get; }
        Prototype.Domain.ItemStack GetSlot(int index);
    }
}
