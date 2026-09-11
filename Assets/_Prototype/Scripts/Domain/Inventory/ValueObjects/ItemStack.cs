namespace Prototype.Domain
{
    /// <summary>Value object representing the item/count pair occupying one inventory slot.</summary>
    public sealed class ItemStack
    {
        public string ItemId { get; }
        public int Count { get; private set; }

        public ItemStack(string id, int count = 1)
        {
            ItemId = id;
            Count = count;
        }

        // Mutation is intentionally small and is called by Infrastructure services only.
        public void Add(int amount) => Count += amount;
        public void Remove(int amount) => Count -= amount;
    }
}
