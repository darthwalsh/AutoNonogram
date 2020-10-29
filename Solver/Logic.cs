using System;
using System.Collections.Generic;
using System.Linq;

namespace Solver
{
  public sealed class Logic
  {
    Puzzle puzzle;
    List<List<Cell>> rows;
    List<List<Cell>> cols;
    public Logic(Puzzle puzzle)
    {
      this.puzzle = puzzle;
      this.rows = Enumerable.Range(1, puzzle.Dim).Select(y => Enumerable.Range(1, puzzle.Dim).Select(x => new Cell()).ToList()).ToList();
      this.cols = rows.Select((row, y) => row.Select((_, x) => rows[x][y]).ToList()).ToList();
    }

    public List<List<Cell>> Solve()
    {
      int debug = 0;
      while (rows.Any(row => row.Any(c => c.IsUnknown)))
      {
        if (++debug > 10000) {
          throw new InvalidOperationException("Broked");
        }

        foreach (var (clues, row) in puzzle.Horizontal.Zip(rows, (p, r) => (p, r)))
        {
          new Guesser(clues, row).Solve();
        }

        foreach (var (clues, col) in puzzle.Vertical.Zip(cols, (p, c) => (p, c)))
        {
          new Guesser(clues, col).Solve();
        }
      }

      return rows;
    }
  }
}
