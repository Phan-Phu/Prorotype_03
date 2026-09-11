namespace Prototype.Application
{
    public readonly struct InventorySlotData
    {
        public readonly string ItemId;
        public readonly int Count;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

        public InventorySlotData(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
