using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Application read adapter over the domain inventory port.</summary>
    public sealed class InventoryQuery : Prototype.Application.IInventoryQuery
    {
        readonly IInventoryReader _reader;

        public InventoryQuery(IInventoryReader reader) => _reader = reader;

        public Prototype.Application.InventorySnapshot Read()
        {
            var slots = new Prototype.Application.InventorySlotData[_reader.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                var stack = _reader.GetSlot(i);
                slots[i] = stack == null
                    ? new Prototype.Application.InventorySlotData(null, 0)
                    : new Prototype.Application.InventorySlotData(stack.ItemId, stack.Count);
            }
            return new Prototype.Application.InventorySnapshot(slots);
        }

        public UniTask<Prototype.Application.InventorySnapshot> ReadAsync()
            => UniTask.FromResult(Read());
    }
}
