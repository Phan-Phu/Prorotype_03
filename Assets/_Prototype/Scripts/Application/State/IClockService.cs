using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Application
{
    public interface IClockService
    {
        UniTask<Result<ClockFailure, GameClockDto>> Read(GameState state);
        UniTask<Result<ClockFailure, GameClockDto>> Tick(GameState state, float deltaSeconds);
        UniTask<Result<ClockFailure, GameClockDto>> ForceEndDay(GameState state);
        UniTask<Result<ClockFailure, GameClockDto>> SkipHour(GameState state);
    }
}
