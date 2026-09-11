namespace Prototype.Domain
{
    /// <summary>
    /// Fixed-slot inventory (AGENT_DEV §3.2). SlotCount=40 matches the InventoryPlayer.png art exactly:
    /// a 10-slot hotbar row + a 3x10 backpack grid below it. Slots are positional (Slots[i] can be
    /// null) so the UI can support drag-to-reorder (Swap) instead of items just being an unordered bag.
    /// </summary>
    public class Inventory : Prototype.Domain.IInventoryReader
    {
        public const int Columns = 12;
        public const int Rows = 4; // 1 hotbar row + 3 backpack rows
        public const int SlotCount = Columns * Rows;

        // Raw master-data fields. Infrastructure is responsible for applying behavior to these
        // values; the setters are reserved for MasterData import/runtime composition.
        // SET chỉ được sử dụng bởi Infrastructure khi import MasterData.
        public int ColumnCount { get; set; } = Columns;
        public int RowCount { get; set; } = Rows;
        public bool[] LockedSlots { get; set; } = new bool[SlotCount];

        public readonly ItemStack[] Slots = new ItemStack[SlotCount];

        int Prototype.Domain.IInventoryReader.SlotCount => SlotCount;

        ItemStack Prototype.Domain.IInventoryReader.GetSlot(int index)
            => index < 0 || index >= SlotCount ? null : Slots[index];

        // Inventory behavior is implemented by Infrastructure.InventoryService.
        // This entity intentionally exposes raw slots only; it does not own commands.
    }
}
