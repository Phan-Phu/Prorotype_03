using System;
using Cysharp.Threading.Tasks;
using Prototype.Application;
using Prototype.Domain;

namespace Prototype.Infrastructure
{
    /// <summary>Maps infrastructure/domain state into application-facing snapshots.</summary>
    public sealed class GameStateApplicationService : IGameStateQuery, IGameTimeUseCase
    {
        readonly GameState _state;
        readonly InventoryService _inventoryService;
        readonly IClockService _clockService;

        public GameStateApplicationService(GameState state, InventoryService inventoryService,
            IClockService clockService)
        {
            _state = state;
            _inventoryService = inventoryService;
            _clockService = clockService;
        }

        public UniTask<Result<StateFailure, GameStateSnapshotDto>> Read()
            => BuildSnapshot("state.read");

        public UniTask<Result<StateFailure, GameStateSnapshotDto>> Advance(AdvanceTimeRequest request)
        {
            if (_state == null)
                return ResultFactory.UniTaskFailure<StateFailure, GameStateSnapshotDto>(StateFailure.NotInitialized());
            return AdvanceInternal(request);
        }

        async UniTask<Result<StateFailure, GameStateSnapshotDto>> AdvanceInternal(AdvanceTimeRequest request)
        {
            var clock = await _clockService.Tick(_state, request.DeltaSeconds);
            if (!clock.IsSuccess)
                return ResultFactory.Failure<StateFailure, GameStateSnapshotDto>(StateFailure.FromClock(clock.Failure));
            return await BuildSnapshot("state.advance");
        }

        async UniTask<Result<StateFailure, GameStateSnapshotDto>> BuildSnapshot(string context)
        {
            if (_state == null || _state.Wallet == null || _state.Stamina == null || _state.InventorySystem == null)
                return ResultFactory.Failure<StateFailure, GameStateSnapshotDto>(StateFailure.NotInitialized(context));
            try
            {
                var clock = await _clockService.Read(_state);
                if (!clock.IsSuccess)
                    return ResultFactory.Failure<StateFailure, GameStateSnapshotDto>(StateFailure.FromClock(clock.Failure));
                return ResultFactory.Success<StateFailure, GameStateSnapshotDto>(
                    new GameStateSnapshotDto(
                        clock.Value,
                        _state.Wallet.Money,
                        _state.Stamina.Current,
                        _state.PlayerPosition,
                        _inventoryService.Read(_state.InventorySystem)));
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return ResultFactory.Failure<StateFailure, GameStateSnapshotDto>(StateFailure.System(context));
            }
        }
    }
}
