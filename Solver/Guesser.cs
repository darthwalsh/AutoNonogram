using System;
using System.Collections.Generic;
using System.Linq;

namespace Solver
{
  public sealed class Guesser
  {
    List<int> clues;
    List<Cell> cells;

    List<int> blockPositions;
    List<int> counts;
    int possibilities;

    public Guesser(List<int> clues, List<Cell> cells)
    {
      this.clues = clues;
      this.cells = cells;
    }

    public void Solve()
    {
      blockPositions = clues.Select(_ => 0).ToList();
      counts = cells.Select(_ => 0).ToList();
      possibilities = 0;
      Solve(0, 0);

      if (possibilities == 0)
      {
        throw new InvalidOperationException();
      }

      for (var i = 0; i < cells.Count; ++i)
      {
        var cell = cells[i];
        if (!cell.IsUnknown) continue;
        if (counts[i] == 0)
        {
          cell.filled = false;
        }
        else if (counts[i] == possibilities)
        {
          cell.filled = true;
        }
      }
    }

    void Solve(int clueI, int cellI)
    {
      if (clueI == clues.Count)
      {
        if (cells.Skip(cellI).All(CanBeWhite))
        {
          ++possibilities;
          foreach (var (clu, pos) in clues.Zip(blockPositions, (c, b) => (c, b)))
          {
            for (var i = 0; i < clu; ++i)
            {
              ++counts[i + pos];
            }
          }
        }
        return;
      }

      var clue = clues[clueI];
      var remaining = cells.Count - clues.Skip(clueI + 1).Sum() - (clues.Count - clueI - 1);
      for (int start = 0; start + cellI + clue <= remaining; ++start)
      {
        var end = start + cellI + clue;
        if (cells.Skip(cellI).Take(start).All(CanBeWhite) &&
            cells.Skip(cellI + start).Take(clue).All(CanBeBlack) &&
            (end == cells.Count || CanBeWhite(cells[end])))
        {
          blockPositions[clueI] = start + cellI;
          Solve(clueI + 1, end + 1);
        }
      }
    }


    static bool CanBeWhite(Cell c) => !c.IsBlack;
    static bool CanBeBlack(Cell c) => !c.IsWhite;
  }
}
