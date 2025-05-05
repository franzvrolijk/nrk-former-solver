namespace Test;

using NRK;

public class BoardTests
{
    [Fact]
    public void ConstructorTest()
    {
        // Test custom board string
        var customBoard = new Board("ooooooooopppppppppbbbbbbbbbgggggggggooooooooopppppppppbbbbbbbbb");
        Assert.Equal(Board.WIDTH, customBoard.Data.Length);

        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Orange)], customBoard.Data[0]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Pink)], customBoard.Data[1]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Blue)], customBoard.Data[2]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Green)], customBoard.Data[3]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Orange)], customBoard.Data[4]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Pink)], customBoard.Data[5]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Blue)], customBoard.Data[6]);
    }

    [Fact]
    public void MakeMoveTest()
    {
        var board = new Board("ooooooooopppppppppbbbbbbbbbgggggggggooooooooopppppppppbbbbbbbbb");

        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Orange)], board.Data[0]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Pink)], board.Data[1]);

        // Test valid move
        board.MakeMove(1, 1);

        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Orange)], board.Data[0]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Empty)], board.Data[1]);
        Assert.Equal([.. Enumerable.Range(0, Board.HEIGHT).Select(_ => Point.Blue)], board.Data[2]);

        // Test invalid move (empty cell)
        Assert.Throws<InvalidOperationException>(() => board.MakeMove(1, 1));
    }

    [Fact]
    public void MakeMoveWithGravityTest()
    {
        var board = new Board("opbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbb");

        board.MakeMove(0, 1);

        for (int i = 0; i < Board.WIDTH; i++)
        {
            Assert.Equal([Point.Empty, Point.Orange, Point.Blue, Point.Blue, Point.Blue, Point.Blue, Point.Blue, Point.Blue, Point.Blue,], board.Data[i]);
        }
    }

    [Fact]
    public void GetGroupTest()
    {
        var board = new Board("opooooooopppppppppbbbbbbbpbgggggggpgooooooooopppppppppbbbbbbbbb");

        // Test getting a group of same-colored pieces
        var group = board.GetGroup(1, 1);

        // Sort the group by x, then y
        var sortedGroup = group.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

        var expectedGroup = new List<Coordinate>
        {
            new(0, 1),
            new(1, 0),
            new(1, 1),
            new(1, 2),
            new(1, 3),
            new(1, 4),
            new(1, 5),
            new(1, 6),
            new(1, 7),
            new(1, 8),
            new(2, 7),
            new(3, 7)
        };

        // Assert lists are equal and in the same order
        Assert.Equal(expectedGroup.Count, sortedGroup.Count);
        for (int i = 0; i < expectedGroup.Count; i++)
        {
            Assert.Equal(expectedGroup[i], sortedGroup[i]);
        }
    }

    [Fact]
    public void CopyTest()
    {
        var originalBoard = new Board("ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo");
        var copiedBoard = originalBoard.Copy();

        // Verify data is equal in value initially
        Assert.Equal(originalBoard.Data, copiedBoard.Data);

        // Make a change to original and verify copy is unaffected
        originalBoard.MakeMove(1, 1);
        Assert.NotEqual(originalBoard.Data, copiedBoard.Data);
    }

    [Fact]
    public void IsOverTest()
    {
        var board = new Board("ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo");

        Assert.False(board.IsSolved());

        board.MakeMove(1, 1);

        Assert.True(board.IsSolved());
    }
}