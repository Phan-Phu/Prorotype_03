using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Application read adapter over Infrastructure inventory projection.</summary>
    public sealed class InventoryQuery : Prototype.Application.IInventoryQuery
    {
        readonly Prototype.Domain.IInventoryService _service;
        readonly Prototype.Domain.Inventory _inventory;

        public InventoryQuery(Prototype.Domain.IInventoryService service, Prototype.Domain.Inventory inventory)
        {
            _service = service;
            _inventory = inventory;
        }

        public Prototype.Application.InventorySnapshot Read() => _service.Read(_inventory);

        public UniTask<Prototype.Application.InventorySnapshot> ReadAsync() => _service.ReadAsync(_inventory);
    }
}
