using System;
using System.Collections.Generic;

namespace Solver
{
  public sealed class Puzzle
  {
    public int Dim { get; set; }
    public List<List<int>> Vertical { get; set; }
    public List<List<int>> Horizontal { get; set; }
  }
}
