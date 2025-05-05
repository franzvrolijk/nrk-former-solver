using System.Collections.Concurrent;
using System.Text;

namespace NRK;

public enum Point : byte
{
    Orange = 0,
    Pink = 1,
    Blue = 2,
    Green = 3,
    Empty = 4
}

public record struct Coordinate(int X, int Y);

// o = orange, p = pink, b = blue, g = green
// orange = 0, pink = 1, blue = 2, green = 3
// s is a string of the board, starting top left and going top to bottom, left to right
public class Board
{
    public const int WIDTH = 7;
    public const int HEIGHT = 9;
    public Point[][] Data { get; } = new Point[WIDTH][];
    public List<Coordinate> Moves { get; } = [];
    public Board(string s)
    {
        if (s.Length != WIDTH * HEIGHT)
        {
            throw new ArgumentException($"Invalid board string length: {s.Length}");
        }

        var letterToNumber = new Dictionary<char, Point>
        {
            { 'o', Point.Orange },
            { 'p', Point.Pink },
            { 'b', Point.Blue },
            { 'g', Point.Green },
        };

        // For each column
        for (int x = 0; x < WIDTH; x++)
        {
            var column = s
                .Substring(x * HEIGHT, HEIGHT)
                .Select(c => letterToNumber[c])
                .ToArray();

            Data[x] = column;
        }
    }

    // Copy constructor, just initialize the inner arrays
    public Board()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            Data[x] = new Point[HEIGHT];
        }
    }

    public string GetMemoKey()
    {
        Span<char> buffer = stackalloc char[WIDTH * HEIGHT];

        var i = 0;

        for (var x = 0; x < WIDTH; x++)
        {
            for (var y = 0; y < HEIGHT; y++)
            {
                buffer[i++] = (char)('0' + (byte)Data[x][y]);
            }
        }

        return new string(buffer);
    }

    public bool IsSolved() => Data.All(column => column.All(point => point == Point.Empty));

    public void MakeMove(int x, int y)
    {
        if (Data[x][y] == Point.Empty)
        {
            throw new InvalidOperationException($"Invalid move: {x}, {y}");
        }

        var group = GetGroup(x, y);

        // Remove the pieces from the board (set to -1)
        foreach (var (gx, gy) in group)
        {
            Data[gx][gy] = Point.Empty;
        }

        ApplyGravity();

        Moves.Add(new Coordinate(x, y));
    }

    // private static readonly ConcurrentDictionary<string, List<Coordinate>> _groupMemo = new();
    public List<Coordinate> GetGroup(int x, int y)
    {
        // var memoKey = $"{GetMemoKey()}_{x}_{y}";
        // if (_groupMemo.TryGetValue(memoKey, out var memoGroup))
        //     return memoGroup;

        var group = new List<Coordinate> { new(x, y) };

        RecurseNeighbors(group, Data[x][y], x, y);

        // group = group.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

        // _groupMemo.TryAdd(memoKey, group);

        return group;
    }

    private void RecurseNeighbors(List<Coordinate> group, Point color, int x, int y)
    {
        var neighbors = new List<Coordinate>
        {
            new(x - 1, y),
            new(x + 1, y),
            new(x, y - 1),
            new(x, y + 1),
        };

        foreach (var (nx, ny) in neighbors)
        {
            var isInBounds = nx >= 0 && nx < WIDTH && ny >= 0 && ny < HEIGHT;
            if (!isInBounds) continue;

            var isSameColor = Data[nx][ny] == color;
            if (!isSameColor) continue;

            var isAlreadyInGroup = group.Any(g => g.X == nx && g.Y == ny);
            if (isAlreadyInGroup) continue;

            group.Add(new(nx, ny));

            RecurseNeighbors(group, color, nx, ny);
        }
    }

    public void ApplyGravity()
    {
        // For each column
        for (int x = 0; x < WIDTH; x++)
        {
            var column = Data[x];
            var insertAt = HEIGHT - 1;

            // Move non-empty points to their new positions
            for (int i = HEIGHT - 1; i >= 0; i--)
            {
                if (column[i] == Point.Empty) continue;

                column[insertAt] = column[i];
                insertAt--;
            }

            // Fill the rest of the column with empty points
            for (int i = insertAt; i >= 0; i--)
            {
                column[i] = Point.Empty;
            }
        }
    }

    public Board Copy()
    {
        var newBoard = new Board();

        for (int x = 0; x < WIDTH; x++)
        {
            newBoard.Data[x] = [.. Data[x]];
        }

        newBoard.Moves.AddRange(Moves);

        return newBoard;
    }

    private static readonly ConcurrentDictionary<string, List<Coordinate>> _prioMemo = new();
    public List<Coordinate> GetPrioritizedMoves()
    {
        // var boardKey = GetMemoKey();
        // if (_prioMemo.TryGetValue(boardKey, out var moves))
        // {
        //     return moves;
        // }

        var distinctMoves = GetDistinctMoves();

        var prioritizedMoves = distinctMoves
            .Select(tuple =>
            {
                var (move, groupSize) = tuple;
                var copy = Copy();
                copy.MakeMove(move.X, move.Y);
                var groupsAfterMove = copy.GetDistinctMoves().Count;
                return (move, groupSize, groupsAfterMove);
            })
            .OrderBy(tuple => tuple.groupsAfterMove)
            .ThenByDescending(tuple => tuple.groupSize)
            .Select(tuple => tuple.move)
            .ToList();

        // _prioMemo.TryAdd(boardKey, prioritizedMoves);

        return prioritizedMoves;
    }

    private List<(Coordinate Move, int GroupSize)> GetDistinctMoves()
    {
        var distinctMoves = new List<(Coordinate Move, int GroupSize)>();
        var visited = new bool[WIDTH, HEIGHT];

        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                if (Data[x][y] == Point.Empty || visited[x, y])
                    continue;

                var group = GetGroup(x, y);

                foreach (var (gx, gy) in group)
                    visited[gx, gy] = true;

                distinctMoves.Add((new Coordinate(x, y), group.Count));
            }
        }

        return distinctMoves;
    }

    public void Print()
    {
        var numberToLetter = new Dictionary<Point, char>
        {
            { Point.Orange, 'O' },
            { Point.Pink, 'P' },
            { Point.Blue, 'B' },
            { Point.Green, 'G' },
            { Point.Empty, ' ' },
        };

        for (int y = 0; y < HEIGHT; y++)
        {
            var s = new StringBuilder();
            for (int x = 0; x < WIDTH; x++)
            {
                var point = Data[x][y];
                var letter = numberToLetter[point];
                s.Append(letter);
            }

            Console.WriteLine(s.ToString());
        }
    }
}