using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    public interface IClockService
    {
        GameClockDto Read(GameState state);
        GameClockDto Tick(GameState state, float deltaSeconds);
        UniTask<GameClockDto> TickAsync(GameState state, float deltaSeconds);
        GameClockDto ForceEndDay(GameState state);
        UniTask<GameClockDto> ForceEndDayAsync(GameState state);
        GameClockDto SkipHour(GameState state);
    }
}
