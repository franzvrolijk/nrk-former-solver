use board::Board;
use solver::Solver;

mod board;
mod solver;

fn main() {
  let s: &str =
    "oobgpbgppogpopbobgpboogppbbgpopogpbpgooopbpppobgpoggpogoggpogpo";

  let board = Board::new(s).unwrap();

  let solver = Solver::new();

  solver.solve(board);

  // let mut board = Board::new(
  //   "gbogbogoppbopgbogggpgbgobbpbopoobobppbpobgoggogoppogbopoppgobbb",
  // )
  // .expect("board");

  // println!("Initial board state:");
  // board.print();

  // let moves = vec![
  //   Coordinate { x: 3, y: 1 },
  //   Coordinate { x: 5, y: 3 },
  //   Coordinate { x: 5, y: 7 },
  //   Coordinate { x: 3, y: 6 },
  //   Coordinate { x: 4, y: 6 },
  //   Coordinate { x: 2, y: 6 },
  //   Coordinate { x: 2, y: 7 },
  //   Coordinate { x: 2, y: 8 },
  //   Coordinate { x: 0, y: 2 },
  //   Coordinate { x: 5, y: 8 },
  //   Coordinate { x: 0, y: 8 },
  //   Coordinate { x: 0, y: 7 },
  //   Coordinate { x: 0, y: 7 },
  //   Coordinate { x: 0, y: 7 },
  //   Coordinate { x: 1, y: 7 },
  //   Coordinate { x: 0, y: 7 },
  // ];

  // for m in moves {
  //   board.make_move(m.x, m.y).expect("move made");
  //   println!("After move: ({}, {})", m.x, m.y);
  //   board.print();
  // }
}
