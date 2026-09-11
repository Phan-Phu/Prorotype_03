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

        public GameStateApplicationService(GameState state, IInventoryQuery inventoryQuery)
        {
            _state = state;
            _inventoryQuery = inventoryQuery;
        }

        public GameStateSnapshotDto Read() => ToSnapshot();

        public GameStateSnapshotDto Advance(AdvanceTimeRequest request)
        {
            _state.Clock.Tick(request.DeltaSeconds);
            return ToSnapshot();
        }

        GameStateSnapshotDto ToSnapshot()
            => new GameStateSnapshotDto(
                new GameClockDto(_state.Clock.Day, _state.Clock.Hour),
                _state.Wallet.Money,
                _state.Stamina.Current,
                _state.PlayerPosition,
                _inventoryQuery.Read());
    }
}
