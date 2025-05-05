using NRK;

var gameString = "oopogbpoobbbobpbgpbbgggbpgbpgbgobooboppobbogpbpgboogpopopbpgggp";

var board = new Board(gameString);

await new Solver().Solve(board);

Console.WriteLine($"\n\n DONE!\n\n");
