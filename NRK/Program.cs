using NRK;

var gameString = "obobobobooboboboboobobobobooboboboboobobobobooboboboboobobobobo";

var board = new Board(gameString);

await new Solver().Solve(board);

Console.WriteLine($"\n\n DONE!\n\n");
