using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Parser;
using Phone;
using Solver;

namespace ANG
{
  class Program
  {
    static async Task Solve(Tapper tapper) {
      var sw = Stopwatch.StartNew();

      var screen = Adb.Screenshot();
      Console.Error.WriteLine($"Screenshot took {sw.ElapsedMilliseconds}ms");
      sw.Restart();

      using (var image = new Bitmap(screen)) {
        var parser = new AndroidParser(image);

        var puzzle = await parser.Parse();
        Console.Error.WriteLine($"Parsing    took {sw.ElapsedMilliseconds}ms");
        sw.Restart();
        
        var solvedRows = new Logic(puzzle).Solve();
        Console.Error.WriteLine($"Solving    took {sw.ElapsedMilliseconds}ms");
        // System.IO.File.WriteAllLines(
        //   @"C:\code\test\DragNonogram\" + Guid.NewGuid().ToString().Replace("-", "") + ".txt",

        //   new[] { puzzle.Dim.ToString() }
        //     .Concat(puzzle.Vertical.Select(col => string.Join(" ", col)))
        //     .Concat(puzzle.Horizontal.Select(row => string.Join(" ", row)))
        //     .Concat(solvedRows.Select(row => string.Concat(row).Replace(" ", ".")))
        // );
        sw.Restart();

        var toFill = solvedRows.SelectMany((row, y) => row
          .Select((c, x) => (c, x))
          .Where(o => o.c.IsBlack)
          .Select(o => parser.getCell(o.x, y)));

        tapper.Tap(toFill.Select(p => (p.X, p.Y)));
        Console.Error.WriteLine($"Tapping    took {sw.ElapsedMilliseconds}ms");
      }
    }

    static async Task Main(string[] args)
    {
      using (var tapper = new Tapper())
      {
        Console.WriteLine("Press Enter repeatedly to run. Any input or ctrl+C to quit");
        
        while (true)
        {
          await Solve(tapper);

          var line = Console.ReadLine();
          if (line != "") break;
        }
      } 
    }
  }
}
