using System;
using System.Collections.Generic;

namespace Solver
{
  public sealed class Logic
  {
    int dim;
    public Logic(int dim) {
      this.dim = dim;
    }

    public void Check(List<Cell> cells) {
      throw new NotImplementedException();
    } 
  }

  public sealed class Cell {
    public bool? filled;
  }
}
