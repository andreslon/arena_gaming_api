using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Core.Application.Interfaces;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Game>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<Game> AddAsync(Game game, CancellationToken cancellationToken = default);
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
} 