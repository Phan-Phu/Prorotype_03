namespace Prototype.Application
{
    public sealed class InventorySnapshot
    {
        public readonly InventorySlotData[] Slots;

        public InventorySnapshot(InventorySlotData[] slots) => Slots = slots;
    }
}
