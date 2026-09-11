using Cysharp.Threading.Tasks;

namespace Prototype.Application
{
    public interface IInventoryQuery
    {
        InventorySnapshot Read();
        UniTask<InventorySnapshot> ReadAsync();
    }
}
