using System;
using System.Collections.Generic;
using System.Linq;

namespace Solver
{
  public sealed class Guesser
  {
    public List<int> clues;
    List<int> blockPos;
    public List<Cell> cells;
    List<int> possCount;
    int posses;

    public void Solve()
    {
      blockPos = clues.Select(c => 0).ToList();
      possCount = Enumerable.Repeat(0, cells.Count).ToList();
      posses = 0;
      Solve(0, 0);

      if (posses == 0)
      {
        throw new InvalidOperationException();
      }

      for (var i = 0; i < cells.Count; ++i)
      {
        var cell = cells[i];
        if (!IsUnknown(cell)) continue;
        if (possCount[i] == 0)
        {
          cell.filled = false;
        }
        else if (possCount[i] == posses)
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
          ++posses;
          foreach (var (c, start) in clues.Zip(blockPos, (c, b) => (c, b)))
          {
            for (var i = 0; i < c; ++i)
            {
              ++possCount[i + start];
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
          blockPos[clueI] = start + cellI;
          Solve(clueI + 1, end + 1);
        }
      }
    }

    public static bool IsWhite(Cell c) => c.filled == false;
    public static bool IsBlack(Cell c) => c.filled == true;
    public static bool IsUnknown(Cell c) => !c.filled.HasValue;
    public static bool CanBeWhite(Cell c) => !IsBlack(c);
    public static bool CanBeBlack(Cell c) => !IsWhite(c);
  }
}
