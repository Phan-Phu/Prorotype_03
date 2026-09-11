using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Application boundary for simulation time and state reads. Domain owns the rules; this service
    /// owns the DTO mapping so Unity views do not depend on mutable domain aggregates.
    /// </summary>
    public sealed class GameStateApplicationService : IGameStateQuery, IGameTimeUseCase
    {
        readonly GameState _state;
        readonly IInventoryQuery _inventoryQuery;
        readonly IClockService _clockService;

        public GameStateApplicationService(GameState state, IInventoryQuery inventoryQuery, IClockService clockService)
        {
            _state = state;
            _inventoryQuery = inventoryQuery;
            _clockService = clockService;
        }

        public GameStateSnapshotDto Read() => ToSnapshot();

        public GameStateSnapshotDto Advance(AdvanceTimeRequest request)
        {
            _clockService.Tick(_state, request.DeltaSeconds);
            return ToSnapshot();
        }

        GameStateSnapshotDto ToSnapshot()
            => new GameStateSnapshotDto(
                _clockService.Read(_state),
                _state.Wallet.Money,
                _state.Stamina.Current,
                _state.PlayerPosition,
                _inventoryQuery.Read());
    }
}
