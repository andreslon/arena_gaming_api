using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Core.Application.Interfaces;

public interface IGeminiService
{
    Task<int> SuggestMoveAsync(Game game);
} 