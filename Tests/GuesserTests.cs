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
    public void ScoreMins()
    {
      Solves(" ", new int[] { });
      Solves("@@", new[] { 2 });
      Solves("@ @", new[] { 1, 1 });
      Solves("@ @@", new[] { 1, 2 });
      Solves("@@ @ @@", new[] { 2, 1, 2 });
    }

    [TestMethod]
    public void ScorePartial()
    {
      Solves(".@.", new[] { 2 });
      Solves("...@.", new[] { 1, 2 });
      Solves(".@..@..@.", new[] { 2, 2, 2 });
    }

    [TestMethod]
    public void ScoreStarting()
    {
      Solves("@@ ", new[] { 2 }, "@..");
      Solves(".@.", new[] { 2 }, ".@.");
      Solves(" @@", new[] { 2 }, "..@");
    }

    static void Solves(string expected, ICollection<int> clues, string start) {
      Assert.AreEqual(expected, Run(clues.ToList(), start));
    }

    static void Solves(string expected, ICollection<int> clues)
    {
      Solves(expected, clues, new String('.', expected.Length));
    }

    static string Run(List<int> clues, string existing)
    {
      var cells = existing.Select(Cell.FromChar).ToList();

      new Guesser
      {
        clues = clues,
        cells = cells,
      }.Solve();

      return string.Join("", cells);
    }
  }
}
