namespace Prototype.Domain
{
    /// <summary>Money holder. Plain C#.</summary>
    public class Wallet
    {
        public int Money;
        public Wallet(int start) { Money = start; }
        public bool TrySpend(int amount) { if (Money < amount) return false; Money -= amount; return true; }
        public void Add(int amount) { Money += amount; }
    }
}
