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

        public readonly ItemStack[] Slots = new ItemStack[SlotCount];

        int Prototype.Domain.IInventoryReader.SlotCount => SlotCount;

        ItemStack Prototype.Domain.IInventoryReader.GetSlot(int index)
            => index < 0 || index >= SlotCount ? null : Slots[index];

        /// <summary>True if Add(itemId) would stack into an existing pile or occupy an empty slot.</summary>
        public bool CanAdd(string itemId)
        {
            for (int i = 0; i < SlotCount; i++)
                if (Slots[i] != null && Slots[i].ItemId == itemId) return true;
            for (int i = 0; i < SlotCount; i++)
                if (Slots[i] == null) return true;
            return false;
        }

        /// <summary>Stacks onto an existing pile of the same item first, else fills the first empty slot. Returns false if full.</summary>
        public bool Add(string itemId, int n = 1)
        {
            for (int i = 0; i < SlotCount; i++)
                if (Slots[i] != null && Slots[i].ItemId == itemId) { Slots[i].Add(n); return true; }
            for (int i = 0; i < SlotCount; i++)
                if (Slots[i] == null) { Slots[i] = new ItemStack(itemId, n); return true; }
            return false;
        }

        public int Count(string itemId)
        {
            int c = 0;
            foreach (var s in Slots) if (s != null && s.ItemId == itemId) c += s.Count;
            return c;
        }

        /// <summary>Consumes n of itemId from wherever it's stacked; returns false (no change) if there isn't enough in any one slot.</summary>
        public bool TryRemove(string itemId, int n = 1)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Slots[i] == null || Slots[i].ItemId != itemId || Slots[i].Count < n) continue;
                Slots[i].Remove(n);
                if (Slots[i].Count <= 0) Slots[i] = null;
                return true;
            }
            return false;
        }

        /// <summary>Swaps the contents of two slots (drag-and-drop reorder in InventoryScreenUI). No-op on an out-of-range index.</summary>
        public void Swap(int a, int b)
        {
            if (a < 0 || a >= SlotCount || b < 0 || b >= SlotCount || a == b) return;
            (Slots[a], Slots[b]) = (Slots[b], Slots[a]);
        }

        public void Clear()
        {
            for (int i = 0; i < SlotCount; i++) Slots[i] = null;
        }
    }
}
