using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Infrastructure implementation of the Domain inventory port. The Application query is a
    /// projection through GenericMapper, so callers never receive the mutable Domain aggregate.
    /// </summary>
    public sealed class InventoryService : Prototype.Domain.IInventoryService, Prototype.Application.IInventoryQuery
    {
        readonly Inventory _boundInventory;

        public InventoryService(Inventory boundInventory = null)
        {
            _boundInventory = boundInventory;
        }

        InventorySlotData[] Prototype.Application.IInventoryQuery.Read()
            => Read(_boundInventory);

        UniTask<InventorySlotData[]> Prototype.Application.IInventoryQuery.ReadAsync()
            => ReadAsync(_boundInventory);

        public InventorySlotData[] Read(Inventory inventory)
            => GenericMapper.Map(inventory, source =>
            {
                if (source == null) return new InventorySlotData[0];

                var slots = new InventorySlotData[source.Slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    var stack = source.Slots[i];
                    slots[i] = stack == null
                        ? new InventorySlotData(null, 0)
                        : new InventorySlotData(stack.ItemId, stack.Count);
                }
                return slots;
            });

        public UniTask<InventorySlotData[]> ReadAsync(Inventory inventory)
            => UniTask.FromResult(Read(inventory));

        public UniTask<bool> Add(Inventory inventory, string itemId, int amount = 1)
            => UniTask.FromResult(AddInternal(inventory, itemId, amount));

        public UniTask<bool> Remove(Inventory inventory, string itemId, int amount = 1)
            => UniTask.FromResult(RemoveInternal(inventory, itemId, amount));

        public UniTask<bool> CanAdd(Inventory inventory, string itemId)
            => UniTask.FromResult(CanAddInternal(inventory, itemId));

        public UniTask<int> Count(Inventory inventory, string itemId)
            => UniTask.FromResult(CountInternal(inventory, itemId));

        public UniTask Clear(Inventory inventory)
        {
            ClearInternal(inventory);
            return UniTask.CompletedTask;
        }

        public UniTask Swap(Inventory inventory, int firstSlot, int secondSlot)
        {
            SwapInternal(inventory, firstSlot, secondSlot);
            return UniTask.CompletedTask;
        }

        bool AddInternal(Inventory inventory, string itemId, int amount)
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

        bool RemoveInternal(Inventory inventory, string itemId, int amount)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId) || amount <= 0) return false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (IsLocked(inventory, i) || stack == null || stack.ItemId != itemId || stack.Count < amount)
                    continue;
                stack.Remove(amount);
                if (stack.Count <= 0) inventory.Slots[i] = null;
                return true;
            }
            return false;
        }

        bool CanAddInternal(Inventory inventory, string itemId)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId)) return false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                if (!IsLocked(inventory, i) && (inventory.Slots[i] == null || inventory.Slots[i].ItemId == itemId))
                    return true;
            }
            return false;
        }

        int CountInternal(Inventory inventory, string itemId)
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

        void ClearInternal(Inventory inventory)
        {
            if (inventory == null) return;
            for (int i = 0; i < inventory.Slots.Length; i++)
                if (!IsLocked(inventory, i)) inventory.Slots[i] = null;
        }

        void SwapInternal(Inventory inventory, int firstSlot, int secondSlot)
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
