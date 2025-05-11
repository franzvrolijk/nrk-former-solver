use board::Board;

mod board;

fn main() {
    let s: &str = "ooopbgpobbogpbogobppgbobopogggopbgpbpoobpggpoopppppbbopbpobpooo";

    let board = Board::new(s);
}