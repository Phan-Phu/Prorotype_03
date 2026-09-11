using System;
using Cysharp.Threading.Tasks;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Infrastructure implementation of the Domain inventory port. Mutating operations complete
    /// with an explicit success or a machine-readable failure; expected gameplay failures do not
    /// throw exceptions. Read projections use GenericMapper and remain payload-only.
    /// </summary>
    public sealed class InventoryService : Prototype.Domain.IInventoryService
    {
        readonly Inventory _boundInventory;

        public InventoryService(Inventory boundInventory = null)
        {
            _boundInventory = boundInventory;
        }

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

        public InventorySlotData[] Read()
            => Read(_boundInventory);

        public UniTask<InventorySlotData[]> ReadAsync(Inventory inventory)
            => UniTask.FromResult(Read(inventory));

        public UniTask<OperationResult> Add(Inventory inventory, string itemId, int amount = 1)
            => UniTask.FromResult(SafeAdd(inventory, itemId, amount));

        public UniTask<OperationResult> Remove(Inventory inventory, string itemId, int amount = 1)
            => UniTask.FromResult(SafeRemove(inventory, itemId, amount));

        public UniTask<OperationResult> CanAdd(Inventory inventory, string itemId)
            => UniTask.FromResult(SafeCanAdd(inventory, itemId));

        public UniTask<OperationResult<int>> Count(Inventory inventory, string itemId)
            => UniTask.FromResult(SafeCount(inventory, itemId));

        public UniTask<OperationResult> Clear(Inventory inventory)
            => UniTask.FromResult(SafeClear(inventory));

        public UniTask<OperationResult> Swap(Inventory inventory, int firstSlot, int secondSlot)
            => UniTask.FromResult(SafeSwap(inventory, firstSlot, secondSlot));

        OperationResult SafeAdd(Inventory inventory, string itemId, int amount)
        {
            try { return AddInternal(inventory, itemId, amount); }
            catch (Exception ex) { return SystemFailure("inventory.add", ex); }
        }

        OperationResult SafeRemove(Inventory inventory, string itemId, int amount)
        {
            try { return RemoveInternal(inventory, itemId, amount); }
            catch (Exception ex) { return SystemFailure("inventory.remove", ex); }
        }

        OperationResult SafeCanAdd(Inventory inventory, string itemId)
        {
            try { return CanAddInternal(inventory, itemId); }
            catch (Exception ex) { return SystemFailure("inventory.can_add", ex); }
        }

        OperationResult<int> SafeCount(Inventory inventory, string itemId)
        {
            try { return CountInternal(inventory, itemId); }
            catch (Exception ex)
            {
                return SystemFailure<int>("inventory.count", ex);
            }
        }

        OperationResult SafeClear(Inventory inventory)
        {
            try { return ClearInternal(inventory); }
            catch (Exception ex) { return SystemFailure("inventory.clear", ex); }
        }

        OperationResult SafeSwap(Inventory inventory, int firstSlot, int secondSlot)
        {
            try { return SwapInternal(inventory, firstSlot, secondSlot); }
            catch (Exception ex) { return SystemFailure("inventory.swap", ex); }
        }

        static OperationResult AddInternal(Inventory inventory, string itemId, int amount)
        {
            if (inventory == null) return OperationResult.Failed(FailureCode.NotInitialized, "inventory");
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return OperationResult.Failed(FailureCode.InvalidArgument, "itemId/amount");

            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (!IsLocked(inventory, i) && stack != null && stack.ItemId == itemId)
                {
                    stack.Add(amount);
                    return OperationResult.Success();
                }
            }

            bool hasUnlockedSlot = false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                if (IsLocked(inventory, i)) continue;
                hasUnlockedSlot = true;
                if (inventory.Slots[i] == null)
                {
                    inventory.Slots[i] = new ItemStack(itemId, amount);
                    return OperationResult.Success();
                }
            }

            return OperationResult.Failed(
                hasUnlockedSlot ? FailureCode.InventoryFull : FailureCode.LockedSlot,
                itemId);
        }

        static OperationResult RemoveInternal(Inventory inventory, string itemId, int amount)
        {
            if (inventory == null) return OperationResult.Failed(FailureCode.NotInitialized, "inventory");
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return OperationResult.Failed(FailureCode.InvalidArgument, "itemId/amount");

            int total = 0;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (!IsLocked(inventory, i) && stack != null && stack.ItemId == itemId)
                    total += stack.Count;
            }

            if (total == 0) return OperationResult.Failed(FailureCode.ItemNotFound, itemId);
            if (total < amount)
                return OperationResult.Failed(FailureCode.InsufficientInventory, itemId, amount, total);

            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (IsLocked(inventory, i) || stack == null || stack.ItemId != itemId || stack.Count < amount)
                    continue;
                stack.Remove(amount);
                if (stack.Count <= 0) inventory.Slots[i] = null;
                return OperationResult.Success();
            }

            return OperationResult.Failed(FailureCode.InvalidState, itemId);
        }

        static OperationResult CanAddInternal(Inventory inventory, string itemId)
        {
            if (inventory == null) return OperationResult.Failed(FailureCode.NotInitialized, "inventory");
            if (string.IsNullOrEmpty(itemId))
                return OperationResult.Failed(FailureCode.InvalidArgument, "itemId");

            bool hasUnlockedSlot = false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                if (IsLocked(inventory, i)) continue;
                hasUnlockedSlot = true;
                if (inventory.Slots[i] == null || inventory.Slots[i].ItemId == itemId)
                    return OperationResult.Success();
            }

            return OperationResult.Failed(
                hasUnlockedSlot ? FailureCode.InventoryFull : FailureCode.LockedSlot,
                itemId);
        }

        static OperationResult<int> CountInternal(Inventory inventory, string itemId)
        {
            if (inventory == null) return OperationResult<int>.Failed(FailureCode.NotInitialized, "inventory");
            if (string.IsNullOrEmpty(itemId))
                return OperationResult<int>.Failed(FailureCode.InvalidArgument, "itemId");

            int total = 0;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (stack != null && stack.ItemId == itemId) total += stack.Count;
            }
            return OperationResult<int>.Success(total);
        }

        static OperationResult ClearInternal(Inventory inventory)
        {
            if (inventory == null) return OperationResult.Failed(FailureCode.NotInitialized, "inventory");
            for (int i = 0; i < inventory.Slots.Length; i++)
                if (!IsLocked(inventory, i)) inventory.Slots[i] = null;
            return OperationResult.Success();
        }

        static OperationResult SwapInternal(Inventory inventory, int firstSlot, int secondSlot)
        {
            if (inventory == null) return OperationResult.Failed(FailureCode.NotInitialized, "inventory");
            if (firstSlot < 0 || secondSlot < 0
                || firstSlot >= inventory.Slots.Length || secondSlot >= inventory.Slots.Length
                || firstSlot == secondSlot)
                return OperationResult.Failed(FailureCode.InvalidArgument, "slot");
            if (IsLocked(inventory, firstSlot) || IsLocked(inventory, secondSlot))
                return OperationResult.Failed(FailureCode.LockedSlot, "slot");

            (inventory.Slots[firstSlot], inventory.Slots[secondSlot]) =
                (inventory.Slots[secondSlot], inventory.Slots[firstSlot]);
            return OperationResult.Success();
        }

        static bool IsLocked(Inventory inventory, int slot)
            => inventory.LockedSlots != null && slot >= 0 && slot < inventory.LockedSlots.Length
                && inventory.LockedSlots[slot];

        static OperationResult SystemFailure(string context, Exception exception)
        {
            Debug.LogException(exception);
            return OperationResult.Failed(FailureCode.SystemError, context);
        }

        static OperationResult<T> SystemFailure<T>(string context, Exception exception)
        {
            Debug.LogException(exception);
            return OperationResult<T>.Failed(FailureCode.SystemError, context);
        }
    }
}
