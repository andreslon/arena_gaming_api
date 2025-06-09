using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Core.Application.Interfaces;

public interface IMoveRepository
{
    Task<IEnumerable<Move>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<Move> AddAsync(Move move, CancellationToken cancellationToken = default);
} 