namespace Prototype.Application
{
    /// <summary>Application read model for the inventory UI.</summary>
    public sealed class InventorySnapshot
    {
        public readonly InventorySlotData[] Slots;

        public InventorySnapshot(InventorySlotData[] slots) => Slots = slots;
    }
}
