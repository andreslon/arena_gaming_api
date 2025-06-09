using System;
using System.Collections.Generic;
using System.Linq;
using ArenaGaming.Core.Domain.Common;

namespace ArenaGaming.Core.Domain;

public class Game : Entity
{
    public char[] Board { get; private set; }
    public GameStatus Status { get; private set; }
    public char CurrentPlayerSymbol { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid? WinnerId { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public ICollection<Move> Moves { get; private set; }

    public Game(Guid playerId)
    {
        PlayerId = playerId;
        Board = new char[9] { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };
        Status = GameStatus.InProgress;
        CurrentPlayerSymbol = 'X';
        Moves = new List<Move>();
    }

    public void MakeMove(int position, Guid? playerId)
    {
        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Game is not in progress");

        if (position < 0 || position >= 9)
            throw new ArgumentException("Invalid position", nameof(position));

        if (Board[position] != ' ')
            throw new InvalidOperationException("Position is already taken");

        Board[position] = CurrentPlayerSymbol;
        Moves.Add(new Move(Id, position, CurrentPlayerSymbol, playerId));

        if (CheckWinner())
        {
            Status = GameStatus.Ended;
            WinnerId = playerId;
            EndedAt = DateTime.UtcNow;
        }
        else if (IsBoardFull())
        {
            Status = GameStatus.Ended;
            EndedAt = DateTime.UtcNow;
        }
        else
        {
            CurrentPlayerSymbol = CurrentPlayerSymbol == 'X' ? 'O' : 'X';
        }
    }

    private bool CheckWinner()
    {
        // Check rows
        for (int i = 0; i < 9; i += 3)
        {
            if (Board[i] != ' ' && Board[i] == Board[i + 1] && Board[i] == Board[i + 2])
                return true;
        }

        // Check columns
        for (int i = 0; i < 3; i++)
        {
            if (Board[i] != ' ' && Board[i] == Board[i + 3] && Board[i] == Board[i + 6])
                return true;
        }

        // Check diagonals
        if (Board[0] != ' ' && Board[0] == Board[4] && Board[0] == Board[8])
            return true;
        if (Board[2] != ' ' && Board[2] == Board[4] && Board[2] == Board[6])
            return true;

        return false;
    }

    private bool IsBoardFull()
    {
        return !Board.Contains(' ');
    }
}

public enum GameStatus
{
    InProgress,
    Ended
} 