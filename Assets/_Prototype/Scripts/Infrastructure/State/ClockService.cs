using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>Owns clock progression and day-rollover behavior outside the Domain raw model.</summary>
    public sealed class ClockService : IClockService
    {
        public static int WrapHour(int hour) => ((hour % 24) + 24) % 24;

        public GameClockDto Read(GameState state) => ToDto(state.Clock);

        public GameClockDto Tick(GameState state, float deltaSeconds)
        {
            if (state == null || state.Clock == null) return default;
            int hourMs = (int)(BalanceConfig.SecondsPerInGameHour * 1000f);
            state.Clock.AccumulatedMilliseconds += (int)(deltaSeconds < 0f ? 0f : deltaSeconds * 1000f);
            while (state.Clock.AccumulatedMilliseconds >= hourMs)
            {
                state.Clock.AccumulatedMilliseconds -= hourMs;
                AdvanceHour(state);
            }
            return ToDto(state.Clock);
        }

        public UniTask<GameClockDto> TickAsync(GameState state, float deltaSeconds)
            => UniTask.FromResult(Tick(state, deltaSeconds));

        public GameClockDto SkipHour(GameState state)
        {
            if (state != null && state.Clock != null) AdvanceHour(state);
            return Read(state);
        }

        public GameClockDto ForceEndDay(GameState state)
        {
            if (state == null || state.Clock == null) return default;
            EndDay(state);
            return Read(state);
        }

        public UniTask<GameClockDto> ForceEndDayAsync(GameState state)
            => UniTask.FromResult(ForceEndDay(state));

        static void AdvanceHour(GameState state)
        {
            state.Clock.Hour++;
            if (state.Clock.Hour >= BalanceConfig.DayEndHour)
                EndDay(state);
        }

        static void EndDay(GameState state)
        {
            foreach (var tile in state.Grid.AllTiles())
            {
                if (tile.Crop != null)
                {
                    if (tile.Crop.WateredToday) tile.Crop.DaysGrown++;
                    tile.Crop.WateredToday = false;
                }
                if (tile.Object != null && !tile.Object.IsAlive)
                {
                    if (tile.Object.RespawnDaysLeft > 0) tile.Object.RespawnDaysLeft--;
                    if (tile.Object.RespawnDaysLeft <= 0) tile.Object.HP = TreeDefinition.MaxHP;
                }
                tile.IsWatered = false;
            }
            state.Stamina.Current = state.Stamina.Max;
            state.Clock.Day++;
            state.Clock.Hour = BalanceConfig.DayStartHour;
            state.Clock.AccumulatedMilliseconds = 0;
        }

        static GameClockDto ToDto(GameClock clock)
            => clock == null ? default : new GameClockDto(clock.Day, clock.Hour);
    }
}
