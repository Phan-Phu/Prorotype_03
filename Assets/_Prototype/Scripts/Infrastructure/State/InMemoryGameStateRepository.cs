using Prototype.Domain;

namespace Prototype.Application
{
    /// <summary>
    /// Runtime adapter for the domain repository port. It is intentionally in-memory for the
    /// prototype; a file/cloud adapter can replace it without changing Domain entities.
    /// </summary>
    public sealed class InMemoryGameStateRepository : IGameStateRepository
    {
        GameState _current;

        public GameState Load(int width, int height, int seed)
        {
            _current = new GameState(width, height, seed);
            return _current;
        }

        public void Save(GameState state) => _current = state;
    }
}
