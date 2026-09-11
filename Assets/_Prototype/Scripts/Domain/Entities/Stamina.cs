namespace Prototype.Domain
{
    /// <summary>Stamina pool. Plain C#. Refills at the start of each in-game day.</summary>
    public class Stamina
    {
        public int Current;
        public readonly int Max;

        public Stamina(int max) { Max = max; Current = max; }

        public bool TrySpend(int cost)
        {
            if (Current < cost) return false;
            Current -= cost;
            return true;
        }

        public void Refill() => Current = Max;
    }
}
