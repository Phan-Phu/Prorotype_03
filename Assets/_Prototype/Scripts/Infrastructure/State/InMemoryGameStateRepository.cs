using System;
using Cysharp.Threading.Tasks;
using Prototype.Domain;

namespace Prototype.Infrastructure
{
    /// <summary>In-memory infrastructure adapter for the domain repository port.</summary>
    public sealed class InMemoryGameStateRepository : IGameStateRepository
    {
        GameState _current;

        public UniTask<Result<RepositoryFailure, GameState>> Load(int width, int height, int seed)
        {
            if (width <= 0 || height <= 0)
                return ResultFactory.UniTaskFailure<RepositoryFailure, GameState>(RepositoryFailure.InvalidArgument("grid_size"));
            try
            {
                _current = new GameState(width, height, seed);
                return ResultFactory.UniTaskSuccess<RepositoryFailure, GameState>(_current);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return ResultFactory.UniTaskFailure<RepositoryFailure, GameState>(RepositoryFailure.System("repository.load"));
            }
        }

        public UniTask<Result<RepositoryFailure, Unit>> Save(GameState state)
        {
            if (state == null)
                return ResultFactory.UniTaskFailure<RepositoryFailure>(RepositoryFailure.InvalidArgument("state"));
            _current = state;
            return ResultFactory.UniTaskSuccess<RepositoryFailure, Unit>(Unit.Value);
        }
    }
}
