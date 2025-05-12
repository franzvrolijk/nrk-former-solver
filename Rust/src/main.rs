use board::Board;
use solver::Solver;

mod board;
mod solver;

fn main() {
  let s: &str =
    "ogbbbobbgpogogobobpobooooggooopgbppbgoobbooggbpoppgogbpbopobppb";

  let board = Board::new(s).unwrap();

  let solver = Solver::new();

  solver.solve(board);
}
