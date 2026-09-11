using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Inventory behavior and DTO projection. UniTask is used at the application boundary so the
    /// same service can later read from MasterData/save storage without changing UI callers.
    /// </summary>
    public sealed class InventoryService : IInventoryService
    {
        public InventorySnapshot Read(Inventory inventory)
        {
            if (inventory == null) return new InventorySnapshot(new InventorySlotData[0]);
            var slots = new InventorySlotData[inventory.Slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                slots[i] = stack == null
                    ? new InventorySlotData(null, 0)
                    : new InventorySlotData(stack.ItemId, stack.Count);
            }
            return new InventorySnapshot(slots);
        }

        public UniTask<InventorySnapshot> ReadAsync(Inventory inventory)
            => UniTask.FromResult(Read(inventory));

        public bool Add(Inventory inventory, string itemId, int amount = 1)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId) || amount <= 0) return false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (!IsLocked(inventory, i) && stack != null && stack.ItemId == itemId)
                {
                    stack.Add(amount);
                    return true;
                }
            }

            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                if (!IsLocked(inventory, i) && inventory.Slots[i] == null)
                {
                    inventory.Slots[i] = new ItemStack(itemId, amount);
                    return true;
                }
            }
            return false;
        }

        public bool Remove(Inventory inventory, string itemId, int amount = 1)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId) || amount <= 0) return false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (IsLocked(inventory, i) || stack == null || stack.ItemId != itemId || stack.Count < amount) continue;
                stack.Remove(amount);
                if (stack.Count <= 0) inventory.Slots[i] = null;
                return true;
            }
            return false;
        }

        public bool CanAdd(Inventory inventory, string itemId)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId)) return false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                if (!IsLocked(inventory, i) && (inventory.Slots[i] == null || inventory.Slots[i].ItemId == itemId)) return true;
            }
            return false;
        }

        public int Count(Inventory inventory, string itemId)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId)) return 0;
            int total = 0;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (stack != null && stack.ItemId == itemId) total += stack.Count;
            }
            return total;
        }

        public void Clear(Inventory inventory)
        {
            if (inventory == null) return;
            for (int i = 0; i < inventory.Slots.Length; i++)
                if (!IsLocked(inventory, i)) inventory.Slots[i] = null;
        }

        public void Swap(Inventory inventory, int firstSlot, int secondSlot)
        {
            if (inventory == null || firstSlot < 0 || secondSlot < 0
                || firstSlot >= inventory.Slots.Length || secondSlot >= inventory.Slots.Length
                || firstSlot == secondSlot) return;
            if (IsLocked(inventory, firstSlot) || IsLocked(inventory, secondSlot)) return;
            (inventory.Slots[firstSlot], inventory.Slots[secondSlot]) =
                (inventory.Slots[secondSlot], inventory.Slots[firstSlot]);
        }

        static bool IsLocked(Inventory inventory, int slot)
            => inventory.LockedSlots != null && slot >= 0 && slot < inventory.LockedSlots.Length
                && inventory.LockedSlots[slot];
    }
}
