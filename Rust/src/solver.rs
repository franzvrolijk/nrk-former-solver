use rayon::iter::{IntoParallelRefIterator, ParallelIterator};

use crate::Board;
use crate::board::Coordinate;

use std::{
  collections::{HashMap, hash_map::Entry},
  sync::{
    Arc, RwLock,
    atomic::{AtomicUsize, Ordering},
  },
};

pub struct Solver {
  processed: RwLock<HashMap<Box<[u8]>, usize>>,
  best_so_far: Arc<AtomicUsize>,
}

impl Solver {
  const MAX_DEPTH: usize = 13;

  pub fn new() -> Self {
    Solver {
      best_so_far: Arc::new(AtomicUsize::new(100)),
      processed: RwLock::new(HashMap::new()),
    }
  }

  pub fn solve(&self, board: Board) -> Option<Vec<Coordinate>> {
    let all_moves = board.get_prioritized_moves();

    all_moves
      .par_iter()
      .map(|m| {
        let mut clone = board.clone();
        clone.make_move(m.x, m.y).unwrap();
        self.solve_recursive(&clone)
      })
      .filter_map(|x| x)
      .min_by_key(|solution| solution.len())
  }

  fn solve_recursive(&self, board: &Board) -> Option<Vec<Coordinate>> {
    if board.is_solved() {
      return Some(board.moves.clone());
    }

    let move_count = board.moves.len();

    let reached_max_depth = move_count >= Solver::MAX_DEPTH;
    let already_found_better =
      move_count >= self.best_so_far.load(Ordering::SeqCst);

    if reached_max_depth || already_found_better {
      return None;
    }

    let board_key = board.get_memo_key();

    if !self.should_process(&board_key, &move_count) {
      return None;
    }

    let mut local_best_moves: Option<Vec<Coordinate>> = None;

    let prioritized_moves = board.get_prioritized_moves();

    for m in prioritized_moves {
      let mut clone = board.clone();
      clone.make_move(m.x, m.y).unwrap();

      let result = self.solve_recursive(&clone);

      if result.is_some()
        && (local_best_moves.is_none()
          || result.as_ref().unwrap().len()
            < local_best_moves.as_ref().unwrap().len())
      {
        local_best_moves = result;
      }
    }

    if local_best_moves.is_none() {
      return None;
    }

    self.update_best_so_far(local_best_moves.as_ref().unwrap());

    return local_best_moves;
  }

  fn update_best_so_far(&self, local_best_moves: &Vec<Coordinate>) {
    let new = local_best_moves.len();

    loop {
      let current = self.best_so_far.load(Ordering::SeqCst);
      if new >= current {
        break;
      }

      match self.best_so_far.compare_exchange(
        current,
        new,
        Ordering::SeqCst,
        Ordering::SeqCst,
      ) {
        Ok(_) => {
          println!(
            "New best ({}): {}",
            new,
            local_best_moves
              .iter()
              .map(|m| format!("({}, {})", m.x, m.y))
              .collect::<Vec<String>>()
              .join(", ")
          );

          break;
        }
        Err(_) => continue,
      }
    }
  }

  fn should_process(&self, board_key: &[u8], move_count: &usize) -> bool {
    // If another thread has already processed this board
    if let Ok(map) = self.processed.read() {
      if let Some(&existing_count) = map.get(board_key) {
        // and that thread's move count at that point was <= ours, this board can't possibly lead to a better solution (skip processing)
        if *move_count >= existing_count {
          return false;
        }

        drop(map);
      }
    }

    // Otherwise
    if let Ok(mut map) = self.processed.write() {
      match map.entry(board_key.into()) {
        // Our move count is lower, so we update the existing move count and continue processing
        Entry::Occupied(mut entry) => {
          let existing_count = entry.get();
          // Unless it has changed since we last checked
          if *move_count >= *existing_count {
            return false;
          } else {
            entry.insert(*move_count);
            return true;
          }
        }
        // There is no entry for this board key, so continue processing
        Entry::Vacant(entry) => {
          entry.insert(*move_count);
          return true;
        }
      }
    } else {
      print!("Oops! Poisoned lock. Trying again.");
      return self.should_process(board_key, move_count);
    }
  }
}

#[cfg(test)]
mod tests {
  use super::*;

  #[test]
  fn solve() {
    let board = Board::new(
      "obobobobooboboboboobobobobooboboboboobobobobooboboboboobobobobo",
    )
    .unwrap();

    let solver = Solver::new();

    let solution = solver.solve(board).expect("Solution");

    assert_eq!(solution.len(), 5);
  }
}
