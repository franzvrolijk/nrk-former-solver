using NRK;

var gameString = "ogbbbobbgpogogobobpobooooggooopgbppbgoobbooggbpoppgogbpbopobppb";

var board = new Board(gameString);

await new Solver().Solve(board);

Console.WriteLine($"\n\n DONE!\n\n");
