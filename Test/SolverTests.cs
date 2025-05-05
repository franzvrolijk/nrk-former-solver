namespace Test;

using NRK;

public class SolverTests
{
    [Fact]
    public async Task SolveSimpleBoardTest()
    {
        // Create a simple board that can be solved in 2 moves
        var board = new Board("ooooooooooooooooooooooooooooooooooooppppppppppppppppppppppppppp");

        var solution = await new Solver().Solve(board);

        Assert.Equal(2, solution.Count);
    }

    [Fact]
    public async Task ComplexBoardTest()
    {
        var board = new Board("obobobobooboboboboobobobobooboboboboobobobobooboboboboobobobobo");

        var solution = await new Solver().Solve(board);

        Assert.Equal(5, solution.Count);
    }
}