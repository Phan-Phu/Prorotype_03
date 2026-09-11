namespace Prototype.Domain
{
    /// <summary>
    /// Raw fixed-slot inventory state. Behavior is implemented by Infrastructure.InventoryService.
    /// </summary>
    public class Inventory : IInventoryReader
    {
        public const int Columns = 12;
        public const int Rows = 4;
        public const int SlotCount = Columns * Rows;

        // SET chỉ được sử dụng bởi Infrastructure khi import MasterData hoặc runtime composition.
        public int ColumnCount { get; set; } = Columns;
        // SET chỉ được sử dụng bởi Infrastructure khi import MasterData hoặc runtime composition.
        public int RowCount { get; set; } = Rows;
        // SET chỉ được sử dụng bởi Infrastructure khi import MasterData hoặc runtime composition.
        public bool[] LockedSlots { get; set; } = new bool[SlotCount];

        public readonly ItemStack[] Slots = new ItemStack[SlotCount];

        int IInventoryReader.SlotCount => Slots.Length;

        ItemStack IInventoryReader.GetSlot(int index)
            => index < 0 || index >= Slots.Length ? null : Slots[index];
    }
}
