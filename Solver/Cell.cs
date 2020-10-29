using System.Collections.Generic;
using System.Linq;

namespace Solver
{
  [System.Diagnostics.DebuggerDisplay("{ToString()}")]
  public sealed class Cell
  {
    static readonly IList<(char c, bool? v)> names;
    static Cell()
    {
      // const UNKNOWN = '.', WHITE = ' ', BLACK = '@';
      names = new List<(char c, bool? v)>
      {
        ('.', null),
        (' ', false),
        ('@', true),
      }.AsReadOnly();
    }

    public bool? filled;

    public bool IsWhite=> filled == false;
    public bool IsBlack=> filled == true;
    public bool IsUnknown=> !filled.HasValue;

    public override string ToString()
    {
      return names.First(o => o.v == filled).c.ToString();
    }

    public static Cell FromChar(char c)
    {
      return new Cell
      {
        filled = names.First(o => o.c == c).v
      };
    }
  }
}
