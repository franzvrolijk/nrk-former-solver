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

public class Board
{
    public const int WIDTH = 7;
    public const int HEIGHT = 9;
    public Point[][] Data { get; } = new Point[WIDTH][];
    public List<Coordinate> Moves { get; } = [];

    /// <param name="s">
    /// A 7*9 (63) character string representation of the board, starting from top left and going down.
    /// 'o' for Orange, 'p' for Pink, 'b' for Blue, and 'g' for Green.
    /// </param>
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

        for (int x = 0; x < WIDTH; x++)
        {
            var column = s
                .Substring(x * HEIGHT, HEIGHT)
                .Select(c => letterToNumber[c])
                .ToArray();

            Data[x] = column;
        }
    }

    // Copy constructor, just initialize the inner arrays, no need to fill them with anything.
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

    public List<Coordinate> GetGroup(int x, int y)
    {
        var group = new List<Coordinate> { new(x, y) };

        RecurseNeighbors(group, Data[x][y], x, y);

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

    private int GetDistinctMovesAfterMove(int x, int y)
    {
        var copy = Copy();
        copy.MakeMove(x, y);

        var groupsAfterMove = copy.GetDistinctMoves().Count;

        return groupsAfterMove;
    }

    public List<Coordinate> GetPrioritizedMoves()
    {
        var distinctMoves = GetDistinctMoves();

        var prioritizedMoves = distinctMoves
            .Select(tuple => (
                move: tuple.Move,
                groupSize: tuple.GroupSize,
                groupsAfterMove: GetDistinctMovesAfterMove(tuple.Move.X, tuple.Move.Y)
            ))
            .OrderBy(tuple => tuple.groupsAfterMove)
            .ThenByDescending(tuple => tuple.groupSize)
            .Select(tuple => tuple.move)
            .ToList();

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