using Cysharp.Threading.Tasks;

namespace Prototype.Domain
{
    /// <summary>Domain port for loading and saving the game aggregate.</summary>
    public interface IGameStateRepository
    {
        UniTask<Result<RepositoryFailure, GameState>> Load(int width, int height, int seed,
            MasterDataSnapshot masterData);
        UniTask<Result<RepositoryFailure, Unit>> Save(GameState state);
    }
}
