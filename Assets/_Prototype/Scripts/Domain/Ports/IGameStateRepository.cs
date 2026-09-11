namespace Prototype.Domain
{
    /// <summary>Domain port for loading and saving the game aggregate.</summary>
    public interface IGameStateRepository
    {
        GameState Load(int width, int height, int seed);
        void Save(GameState state);
    }
}
