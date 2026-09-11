using System;
using Cysharp.Threading.Tasks;
using Prototype.Application;
using Prototype.Domain;

namespace Prototype.Infrastructure
{
    /// <summary>Infrastructure implementation of clock progression and day rollover.</summary>
    public sealed class ClockService : IClockService
    {
        public static int WrapHour(int hour) => ((hour % 24) + 24) % 24;

        public UniTask<Result<ClockFailure, GameClockDto>> Read(GameState state)
        {
            if (state?.Clock == null)
                return ResultFactory.UniTaskFailure<ClockFailure, GameClockDto>(ClockFailure.NotInitialized());
            return ResultFactory.UniTaskSuccess<ClockFailure, GameClockDto>(ToDto(state.Clock));
        }

        public UniTask<Result<ClockFailure, GameClockDto>> Tick(GameState state, float deltaSeconds)
        {
            if (state?.Clock == null)
                return ResultFactory.UniTaskFailure<ClockFailure, GameClockDto>(ClockFailure.NotInitialized());
            if (deltaSeconds < 0f)
                return ResultFactory.UniTaskFailure<ClockFailure, GameClockDto>(ClockFailure.InvalidDelta(deltaSeconds));
            return Safe("clock.tick", () =>
            {
                int hourMs = (int)(BalanceConfig.SecondsPerInGameHour * 1000f);
                state.Clock.AccumulatedMilliseconds += (int)(deltaSeconds * 1000f);
                while (state.Clock.AccumulatedMilliseconds >= hourMs)
                {
                    state.Clock.AccumulatedMilliseconds -= hourMs;
                    AdvanceHour(state);
                }
                return ToDto(state.Clock);
            });
        }

        public UniTask<Result<ClockFailure, GameClockDto>> SkipHour(GameState state)
        {
            if (state?.Clock == null)
                return ResultFactory.UniTaskFailure<ClockFailure, GameClockDto>(ClockFailure.NotInitialized());
            return Safe("clock.skip_hour", () => { AdvanceHour(state); return ToDto(state.Clock); });
        }

        public UniTask<Result<ClockFailure, GameClockDto>> ForceEndDay(GameState state)
        {
            if (state?.Clock == null)
                return ResultFactory.UniTaskFailure<ClockFailure, GameClockDto>(ClockFailure.NotInitialized());
            return Safe("clock.end_day", () => { EndDay(state); return ToDto(state.Clock); });
        }

        static UniTask<Result<ClockFailure, GameClockDto>> Safe(string context, Func<GameClockDto> operation)
        {
            try
            {
                return ResultFactory.UniTaskSuccess<ClockFailure, GameClockDto>(operation());
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return ResultFactory.UniTaskFailure<ClockFailure, GameClockDto>(ClockFailure.System(context));
            }
        }

        static void AdvanceHour(GameState state)
        {
            state.Clock.Hour++;
            if (state.Clock.Hour >= BalanceConfig.DayEndHour) EndDay(state);
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
