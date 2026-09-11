using System;
using Cysharp.Threading.Tasks;
using Prototype.Domain;
using UnityEngine;

namespace Prototype.Application
{
    /// <summary>
    /// Infrastructure implementation of the Domain inventory port. Mutating operations complete
    /// with typed InventoryFailure results; expected gameplay failures do not throw exceptions.
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

        public InventorySlotData[] Read() => Read(_boundInventory);

        public UniTask<InventorySlotData[]> ReadAsync(Inventory inventory)
            => UniTask.FromResult(Read(inventory));

        public UniTask<Result<InventoryFailure, Unit>> Add(Inventory inventory, string itemId, int amount = 1)
            => UniTask.FromResult(SafeAdd(inventory, itemId, amount));

        public UniTask<Result<InventoryFailure, Unit>> Remove(Inventory inventory, string itemId, int amount = 1)
            => UniTask.FromResult(SafeRemove(inventory, itemId, amount));

        public UniTask<Result<InventoryFailure, Unit>> CanAdd(Inventory inventory, string itemId)
            => UniTask.FromResult(SafeCanAdd(inventory, itemId));

        public UniTask<Result<InventoryFailure, int>> Count(Inventory inventory, string itemId)
            => UniTask.FromResult(SafeCount(inventory, itemId));

        public UniTask<Result<InventoryFailure, Unit>> Clear(Inventory inventory)
            => UniTask.FromResult(SafeClear(inventory));

        public UniTask<Result<InventoryFailure, Unit>> Swap(Inventory inventory, int firstSlot, int secondSlot)
            => UniTask.FromResult(SafeSwap(inventory, firstSlot, secondSlot));

        Result<InventoryFailure, Unit> SafeAdd(Inventory inventory, string itemId, int amount)
        {
            try { return AddInternal(inventory, itemId, amount); }
            catch (Exception ex) { return SystemFailure("inventory.add", ex); }
        }

        Result<InventoryFailure, Unit> SafeRemove(Inventory inventory, string itemId, int amount)
        {
            try { return RemoveInternal(inventory, itemId, amount); }
            catch (Exception ex) { return SystemFailure("inventory.remove", ex); }
        }

        Result<InventoryFailure, Unit> SafeCanAdd(Inventory inventory, string itemId)
        {
            try { return CanAddInternal(inventory, itemId); }
            catch (Exception ex) { return SystemFailure("inventory.can_add", ex); }
        }

        Result<InventoryFailure, int> SafeCount(Inventory inventory, string itemId)
        {
            try { return CountInternal(inventory, itemId); }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return ResultFactory.Failure<InventoryFailure, int>(InventoryFailure.System("inventory.count"));
            }
        }

        Result<InventoryFailure, Unit> SafeClear(Inventory inventory)
        {
            try { return ClearInternal(inventory); }
            catch (Exception ex) { return SystemFailure("inventory.clear", ex); }
        }

        Result<InventoryFailure, Unit> SafeSwap(Inventory inventory, int firstSlot, int secondSlot)
        {
            try { return SwapInternal(inventory, firstSlot, secondSlot); }
            catch (Exception ex) { return SystemFailure("inventory.swap", ex); }
        }

        static Result<InventoryFailure, Unit> AddInternal(Inventory inventory, string itemId, int amount)
        {
            if (inventory == null) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.NotInitialized());
            if (string.IsNullOrEmpty(itemId)) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.InvalidArgument("itemId"));
            if (amount <= 0) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.ItemQuantityNegative(itemId, amount));

            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (!IsLocked(inventory, i) && stack != null && stack.ItemId == itemId)
                {
                    stack.Add(amount);
                    return ResultFactory.Success<InventoryFailure, Unit>(Unit.Value);
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
                    return ResultFactory.Success<InventoryFailure, Unit>(Unit.Value);
                }
            }

            return hasUnlockedSlot
                ? ResultFactory.Failure<InventoryFailure>(InventoryFailure.Full(itemId))
                : ResultFactory.Failure<InventoryFailure>(InventoryFailure.Locked());
        }

        static Result<InventoryFailure, Unit> RemoveInternal(Inventory inventory, string itemId, int amount)
        {
            if (inventory == null) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.NotInitialized());
            if (string.IsNullOrEmpty(itemId)) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.InvalidArgument("itemId"));
            if (amount <= 0) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.ItemQuantityNegative(itemId, amount));

            int total = 0;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (!IsLocked(inventory, i) && stack != null && stack.ItemId == itemId) total += stack.Count;
            }

            if (total == 0) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.ItemNotFound(itemId));
            if (total < amount) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.InsufficientQuantity(itemId, amount, total));

            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (IsLocked(inventory, i) || stack == null || stack.ItemId != itemId || stack.Count < amount) continue;
                stack.Remove(amount);
                if (stack.Count <= 0) inventory.Slots[i] = null;
                return ResultFactory.Success<InventoryFailure, Unit>(Unit.Value);
            }

            return ResultFactory.Failure<InventoryFailure>(InventoryFailure.InvalidArgument("inventory state"));
        }

        static Result<InventoryFailure, Unit> CanAddInternal(Inventory inventory, string itemId)
        {
            if (inventory == null) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.NotInitialized());
            if (string.IsNullOrEmpty(itemId)) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.InvalidArgument("itemId"));

            bool hasUnlockedSlot = false;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                if (IsLocked(inventory, i)) continue;
                hasUnlockedSlot = true;
                if (inventory.Slots[i] == null || inventory.Slots[i].ItemId == itemId)
                    return ResultFactory.Success<InventoryFailure, Unit>(Unit.Value);
            }

            return hasUnlockedSlot
                ? ResultFactory.Failure<InventoryFailure>(InventoryFailure.Full(itemId))
                : ResultFactory.Failure<InventoryFailure>(InventoryFailure.Locked());
        }

        static Result<InventoryFailure, int> CountInternal(Inventory inventory, string itemId)
        {
            if (inventory == null) return ResultFactory.Failure<InventoryFailure, int>(InventoryFailure.NotInitialized());
            if (string.IsNullOrEmpty(itemId)) return ResultFactory.Failure<InventoryFailure, int>(InventoryFailure.InvalidArgument("itemId"));

            int total = 0;
            for (int i = 0; i < inventory.Slots.Length; i++)
            {
                var stack = inventory.Slots[i];
                if (stack != null && stack.ItemId == itemId) total += stack.Count;
            }
            return ResultFactory.Success<InventoryFailure, int>(total);
        }

        static Result<InventoryFailure, Unit> ClearInternal(Inventory inventory)
        {
            if (inventory == null) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.NotInitialized());
            for (int i = 0; i < inventory.Slots.Length; i++)
                if (!IsLocked(inventory, i)) inventory.Slots[i] = null;
            return ResultFactory.Success<InventoryFailure, Unit>(Unit.Value);
        }

        static Result<InventoryFailure, Unit> SwapInternal(Inventory inventory, int firstSlot, int secondSlot)
        {
            if (inventory == null) return ResultFactory.Failure<InventoryFailure>(InventoryFailure.NotInitialized());
            if (firstSlot < 0 || secondSlot < 0
                || firstSlot >= inventory.Slots.Length || secondSlot >= inventory.Slots.Length
                || firstSlot == secondSlot)
                return ResultFactory.Failure<InventoryFailure>(InventoryFailure.InvalidArgument("slot"));
            if (IsLocked(inventory, firstSlot) || IsLocked(inventory, secondSlot))
                return ResultFactory.Failure<InventoryFailure>(InventoryFailure.Locked());

            (inventory.Slots[firstSlot], inventory.Slots[secondSlot]) =
                (inventory.Slots[secondSlot], inventory.Slots[firstSlot]);
            return ResultFactory.Success<InventoryFailure, Unit>(Unit.Value);
        }

        static bool IsLocked(Inventory inventory, int slot)
            => inventory.LockedSlots != null && slot >= 0 && slot < inventory.LockedSlots.Length
                && inventory.LockedSlots[slot];

        static Result<InventoryFailure, Unit> SystemFailure(string context, Exception exception)
        {
            Debug.LogException(exception);
            return ResultFactory.Failure<InventoryFailure>(InventoryFailure.System(context));
        }
    }
}
