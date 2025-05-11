using System.Collections.Concurrent;

namespace NRK;

public class Solver
{
    private readonly ConcurrentDictionary<string, int> _processed = new();
    private int _bestSoFar = int.MaxValue;
    private const int _maxDepth = 15;

    public List<Coordinate> SolveSync(Board board)
    {
        var result = SolveRecursive(board);

        if (result is null) return [];

        return result;
    }

    public async Task<List<Coordinate>> Solve(Board board)
    {
        var allMoves = board.GetPrioritizedMoves();
        var tasks = new List<Task<List<Coordinate>?>>();

        foreach (var move in allMoves)
        {
            var boardCopy = board.Copy();
            boardCopy.MakeMove(move.X, move.Y);

            tasks.Add(Task.Run(() => SolveRecursive(boardCopy)));
        }

        var results = await Task.WhenAll(tasks);

        var bestResult = results
            .Where(result => result != null)
            .OrderBy(result => result!.Count)
            .First();

        return bestResult!;
    }

    private List<Coordinate>? SolveRecursive(Board board)
    {
        // Base case: if the board is solved, return the moves made to reach this state
        if (board.IsSolved())
            return board.Moves;

        var moveCount = board.Moves.Count;

        var reachedMaxDepth = moveCount >= _maxDepth;
        var alreadyFoundBetter = moveCount >= _bestSoFar;

        if (reachedMaxDepth || alreadyFoundBetter)
            return null;

        var boardKey = board.GetMemoKey();

        if (!CheckShouldProcessThreadSafe(boardKey, moveCount))
            return null;

        List<Coordinate>? localBestMoves = null;

        var prioritizedMoves = board.GetPrioritizedMoves();

        // Try each possible move
        foreach (var move in prioritizedMoves)
        {
            var boardCopy = board.Copy();
            boardCopy.MakeMove(move.X, move.Y);

            var result = SolveRecursive(boardCopy);

            // Update best result if this is better or first solution
            if (result != null && (localBestMoves == null || result.Count < localBestMoves.Count))
            {
                localBestMoves = result;
            }
        }

        if (localBestMoves == null)
        {
            return null;
        }

        UpdateBestSoFarThreadSafe(localBestMoves);

        return localBestMoves;
    }

    private bool CheckShouldProcessThreadSafe(string boardKey, int currentMoveCount)
    {
        while (true)
        {
            // If another thread has already processed this board
            if (_processed.TryGetValue(boardKey, out var existingMoveCount))
            {
                // and that thread's move count at that point was <= ours, this board can't possibly lead to a better solution (skip processing)
                if (currentMoveCount >= existingMoveCount)
                    return false;

                // Otherwise, update dict with better move count (and if it was updated by another thread in the meantime, check again)
                if (_processed.TryUpdate(boardKey, currentMoveCount, existingMoveCount))
                    return true;
            }
            // If this is the first time we are processing this board, add it to the dictionary
            else
            {
                // And if another thread added it in the meantime, check again
                if (_processed.TryAdd(boardKey, currentMoveCount))
                    return true;
            }
        }
    }

    private void UpdateBestSoFarThreadSafe(List<Coordinate> localBestMoves)
    {
        var oldBest = _bestSoFar;
        var potentialNewBest = localBestMoves.Count;

        while (potentialNewBest < oldBest)
        {
            // Tries to update _bestSoFar to potentialNewBest, but only if _bestSoFar is still equal to oldBest (e.g. no other thread updated it in the meantime)
            // If another thread changed it, originalValue will be whatever _bestSoFar was changed to
            var originalValue = Interlocked.CompareExchange(ref _bestSoFar, potentialNewBest, oldBest);

            // Update failed, try again (if necessary)
            if (originalValue != oldBest)
            {
                oldBest = originalValue;
                continue;
            }

            // Successfully updated _bestSoFar
            Console.WriteLine($"New best solution found with {potentialNewBest} moves: {string.Join(", ", localBestMoves.Select(m => $"({m.X}, {m.Y})"))}");
            break;
        }
    }
}