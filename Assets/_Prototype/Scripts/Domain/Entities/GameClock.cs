namespace Prototype.Domain
{
    /// <summary>Raw clock data. Runtime progression is implemented by Infrastructure.ClockService.</summary>
    public class GameClock
    {
        public int Day { get; set; } = 1;
        // SET chỉ được sử dụng bởi Infrastructure khi import MasterData hoặc điều phối runtime.
        public int Hour { get; set; } = BalanceConfig.DayStartHour;
        // SET chỉ được sử dụng bởi Infrastructure khi import MasterData hoặc điều phối runtime.
        public int AccumulatedMilliseconds { get; set; }
    }
}
