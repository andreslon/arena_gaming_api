using System.Threading;
using System.Threading.Tasks;

namespace ArenaGaming.Core.Application.Interfaces;

public interface IGeminiService
{
    Task<int> GetNextMoveAsync(string board, char playerSymbol, CancellationToken cancellationToken = default);
} 