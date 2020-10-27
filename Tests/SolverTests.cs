using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parser;
using Solver;

namespace Tests
{

  [TestClass]
  public class SolverTests
  {
    [TestMethod]
    public void Score()
    {
      Assert.AreEqual(".#.", Run(new[] { 2 }, 3));


      Assert.AreEqual("#X##", Run(new[] { 1, 2}, 4));
    }

    static string Run(IEnumerable<int> clues, int dim) {
      return Run(clues, new String('.', dim));
    }

    static string Run(IEnumerable<int> clues, string existing)
    {
      var dim = existing.Length;
      var chars = new Dictionary<char, bool?>
      {
        ['.'] = null,
        ['X'] = false,
        ['#'] = true,
      };
      var fromChars = chars.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

      var cells = Enumerable.Range(1, dim).Select(_ => new Cell()).ToList();
      if (existing != null)
      {
        for (int i = 0; i < dim; ++i)
        {
          cells[i].filled = chars[existing[i]];
        }
      }


      var logic = new Logic(dim);
      logic.Check(cells);

      return string.Join("", cells.Select(c => fromChars[c.filled]));
    }
  }
}
