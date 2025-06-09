using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArenaGaming.Core.Application.Interfaces;
using ArenaGaming.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ArenaGaming.Infrastructure.Persistence.Repositories;

public class MoveRepository : IMoveRepository
{
    private readonly ApplicationDbContext _context;

    public MoveRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Move>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Moves
            .Where(m => m.GameId == gameId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Move> AddAsync(Move move, CancellationToken cancellationToken = default)
    {
        await _context.Moves.AddAsync(move, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return move;
    }
} 