using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Domain;

namespace ArenaGaming.Core.Application.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<Session> AddAsync(Session session, CancellationToken cancellationToken = default);
    Task UpdateAsync(Session session, CancellationToken cancellationToken = default);
} 