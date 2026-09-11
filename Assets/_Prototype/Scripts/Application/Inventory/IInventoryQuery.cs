using Cysharp.Threading.Tasks;

namespace Prototype.Application
{
    /// <summary>Application read contract consumed by toolbar/inventory UI.</summary>
    public interface IInventoryQuery
    {
        InventorySnapshot Read();
        UniTask<InventorySnapshot> ReadAsync();
    }
}
