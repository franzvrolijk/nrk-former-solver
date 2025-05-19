use itertools::Itertools;
use std::collections::{HashMap, HashSet};

#[repr(u8)]
#[derive(Debug, Clone, Copy, PartialEq)]
pub enum Point {
  Orange,
  Pink,
  Blue,
  Green,
  Empty,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct Coordinate {
  pub x: usize,
  pub y: usize,
}

#[derive(Clone)]
pub struct Board {
  pub data: Vec<Point>, // 2D array flattened to 1D
  pub moves: Vec<Coordinate>,
}

#[derive(Debug)]
pub enum BoardError {
  InvalidBoardStringLength,
  InvalidMove,
}

impl Board {
  pub const WIDTH: usize = 7;
  pub const HEIGHT: usize = 9;

  pub fn new(s: &str) -> Result<Self, BoardError> {
    if s.len() != Self::HEIGHT * Self::WIDTH {
      return Err(BoardError::InvalidBoardStringLength);
    }

    let char_to_point: HashMap<char, Point> = [
      ('o', Point::Orange),
      ('p', Point::Pink),
      ('g', Point::Green),
      ('b', Point::Blue),
    ]
    .into_iter()
    .collect();

    let mut data: Vec<Point> = Vec::with_capacity(Self::WIDTH * Self::HEIGHT);

    for c in s.chars() {
      let point = char_to_point.get(&c).copied().unwrap_or(Point::Empty);

      data.push(point);
    }

    Ok(Board { data, moves: Vec::new() })
  }

  // pub fn print(&self) {
  //   for y in 0..Self::HEIGHT {
  //     for x in 0..Self::WIDTH {
  //       let point = self.data[Board::get_index(x, y)];
  //       let symbol = match point {
  //         Point::Orange => 'o',
  //         Point::Pink => 'p',
  //         Point::Blue => 'b',
  //         Point::Green => 'g',
  //         Point::Empty => '_',
  //       };
  //       print!("{} ", symbol);
  //     }
  //     println!();
  //   }
  // }

  fn get_index(x: usize, y: usize) -> usize {
    (x * Self::HEIGHT) + y
  }

  pub fn get_memo_key(&self) -> &[u8] {
    unsafe {
      std::slice::from_raw_parts(
        self.data.as_ptr() as *const u8,
        self.data.len(),
      )
    }
  }

  pub fn is_solved(&self) -> bool {
    self.data.iter().all(|&x| x == Point::Empty)
  }

  pub fn make_move(&mut self, x: usize, y: usize) -> Result<(), BoardError> {
    if self.data[Board::get_index(x, y)] == Point::Empty {
      return Err(BoardError::InvalidMove);
    }

    let group = self.get_group(x, y);

    for Coordinate { x: gx, y: gy } in group {
      self.data[Board::get_index(gx, gy)] = Point::Empty;
    }

    self.apply_gravity();

    self.moves.push(Coordinate { x, y });

    Ok(())
  }

  pub fn get_group(&self, x: usize, y: usize) -> HashSet<Coordinate> {
    let mut group = HashSet::from([Coordinate { x, y }]);

    self.recurse_neighbors(
      &mut group,
      self.data[Board::get_index(x, y)],
      x,
      y,
    );

    return group;
  }

  fn recurse_neighbors(
    &self,
    group: &mut HashSet<Coordinate>,
    color: Point,
    x: usize,
    y: usize,
  ) {
    // TODO - This may create usizes less than 0
    let mut neighbors: Vec<Coordinate> = Vec::with_capacity(4);

    if x > 0 {
      neighbors.push(Coordinate { x: x - 1, y });
    }
    if x < Board::WIDTH - 1 {
      neighbors.push(Coordinate { x: x + 1, y });
    }
    if y > 0 {
      neighbors.push(Coordinate { x, y: y - 1 });
    }
    if y < Board::HEIGHT - 1 {
      neighbors.push(Coordinate { x, y: y + 1 });
    }

    for Coordinate { x: nx, y: ny } in neighbors {
      let is_same_color = self.data[Board::get_index(nx, ny)] == color;
      if !is_same_color {
        continue;
      }

      if !group.insert(Coordinate { x: nx, y: ny }) {
        continue;
      }

      self.recurse_neighbors(group, color, nx, ny);
    }
  }

  fn apply_gravity(&mut self) {
    for x in 0..Board::WIDTH {
      let column_start = x * Board::HEIGHT;
      let column = &mut self.data[column_start..column_start + Board::HEIGHT];
      let mut insert_at: i32 = (Board::HEIGHT - 1).try_into().unwrap(); // 8

      // [0, 1, ..., 8] (rev)
      for i in (0..Board::HEIGHT).rev() {
        if column[i] == Point::Empty {
          continue;
        }

        column[insert_at as usize] = column[i];
        insert_at -= 1;
      }

      for i in (0..insert_at + 1).rev() {
        column[i as usize] = Point::Empty;
      }
    }
  }

  pub fn get_distinct_moves(&self) -> Vec<(Coordinate, usize)> {
    let mut distinct_moves: Vec<(Coordinate, usize)> = Vec::new();
    let mut visited: HashSet<Coordinate> = HashSet::new();

    for x in 0..Board::WIDTH {
      for y in 0..Board::HEIGHT {
        let is_empty = self.data[Board::get_index(x, y)] == Point::Empty;
        if is_empty {
          continue;
        }

        let already_visited = !visited.insert(Coordinate { x, y });
        if already_visited {
          continue;
        }

        let group = self.get_group(x, y);

        distinct_moves.push((Coordinate { x, y }, group.len()));
      }
    }

    return distinct_moves;
  }

  fn get_distinct_moves_after_move(&self, x: usize, y: usize) -> usize {
    let mut clone = self.clone();

    clone.make_move(x, y).expect("Move was invalid");

    let groups_after_move = clone.get_distinct_moves().len();

    return groups_after_move;
  }

  pub fn get_prioritized_moves(&self) -> Vec<Coordinate> {
    let distinct_moves = self.get_distinct_moves();

    distinct_moves
      .iter()
      .map(|&m| {
        (
          m.0,
          m.1,
          self.get_distinct_moves_after_move(m.0.x, m.0.y),
        )
      })
      .sorted_by(|a, b| a.2.cmp(&b.2).then(b.1.cmp(&a.1)))
      .map(|(coord, _, _)| coord)
      .collect()
  }
}

#[cfg(test)]
mod tests {
  use super::*;

  #[test]
  fn constructor() {
    let board = Board::new(
      "ooooooooopppppppppbbbbbbbbbgggggggggooooooooopppppppppbbbbbbbbb",
    )
    .expect("board");

    let expected = [
      (Point::Orange, 9),
      (Point::Pink, 9),
      (Point::Blue, 9),
      (Point::Green, 9),
      (Point::Orange, 9),
      (Point::Pink, 9),
      (Point::Blue, 9),
    ];

    for (chunk, (expected_point, size)) in board.data.chunks(9).zip(expected) {
      assert!(chunk.iter().all(|&p| p == expected_point));
      assert!(chunk.len() == size)
    }
  }

  #[test]
  fn make_move() {
    let mut board = Board::new(
      "opbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbbopbbbbbbb",
    )
    .expect("board");

    board.make_move(0, 1).expect("move made");

    let expected_rows = [
      vec![Point::Empty; Board::WIDTH],
      vec![Point::Orange; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
      vec![Point::Blue; Board::WIDTH],
    ];

    for y in 0..Board::HEIGHT {
      let row: Vec<Point> =
        (0..Board::WIDTH).map(|x| board.data[Board::get_index(x, y)]).collect();

      assert_eq!(
        expected_rows[y], row,
        "Row {} does not match expected",
        y
      );
    }
  }

  #[test]
  fn get_group() {
    let board = Board::new(
      "opooooooopppppppppbbbbbbbpbgggggggpgooooooooopppppppppbbbbbbbbb",
    )
    .expect("board");

    let group = board.get_group(1, 1);

    let sorted_group: Vec<Coordinate> = group
      .into_iter()
      .sorted_by(|a, b| a.x.cmp(&b.x).then(a.y.cmp(&b.y)))
      .collect();

    let expected_group: Vec<Coordinate> = vec![
      Coordinate { x: 0, y: 1 },
      Coordinate { x: 1, y: 0 },
      Coordinate { x: 1, y: 1 },
      Coordinate { x: 1, y: 2 },
      Coordinate { x: 1, y: 3 },
      Coordinate { x: 1, y: 4 },
      Coordinate { x: 1, y: 5 },
      Coordinate { x: 1, y: 6 },
      Coordinate { x: 1, y: 7 },
      Coordinate { x: 1, y: 8 },
      Coordinate { x: 2, y: 7 },
      Coordinate { x: 3, y: 7 },
    ];

    assert_eq!(sorted_group, expected_group);
  }

  #[test]
  fn copy() {
    let mut original = Board::new(
      "ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
    )
    .expect("board");

    let other = original.clone();

    assert_eq!(original.data, other.data);
    assert_eq!(original.moves, other.moves);

    original.make_move(1, 1).expect("move made");

    assert_ne!(original.data, other.data);
    assert_ne!(original.moves, other.moves);
  }

  #[test]
  fn is_solved() {
    let mut board = Board::new(
      "ooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo",
    )
    .expect("board");

    assert_eq!(board.is_solved(), false);

    board.make_move(1, 1).expect("move made");

    assert_eq!(board.is_solved(), true);
  }

  #[test]
  fn bug_scenario() {
    let mut board = Board::new(
      "gbogbogoppbopgbogggpgbgobbpbopoobobppbpobgoggogoppogbopoppgobbb",
    )
    .expect("board");

    let moves = vec![
      Coordinate { x: 3, y: 4 },
      Coordinate { x: 4, y: 3 },
      Coordinate { x: 5, y: 7 },
      Coordinate { x: 3, y: 6 },
      Coordinate { x: 4, y: 6 },
      Coordinate { x: 2, y: 6 },
      Coordinate { x: 2, y: 8 },
      Coordinate { x: 1, y: 7 },
      Coordinate { x: 0, y: 6 },
      Coordinate { x: 1, y: 6 },
      Coordinate { x: 0, y: 5 },
      Coordinate { x: 5, y: 8 },
      Coordinate { x: 0, y: 5 },
      Coordinate { x: 0, y: 8 },
      Coordinate { x: 0, y: 6 },
      Coordinate { x: 0, y: 8 },
    ];

    for m in moves {
      board.make_move(m.x, m.y).expect("move made");
    }

    assert_eq!(board.is_solved(), false);
  }
}
