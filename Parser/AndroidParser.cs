using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ImageParse;
using Solver;

namespace Parser
{
  public class AndroidParser
  {
    static Dictionary<char, Bitmap> digitImgs;

    static AndroidParser()
    {
      digitImgs = new Dictionary<char, Bitmap>();
      var assembly = System.Reflection.Assembly.GetExecutingAssembly();
      foreach (var name in assembly.GetManifestResourceNames())
      {
        var rank = name.Replace("Parser.img.", "").Replace(".png", "").Single();
        using (var stream = assembly.GetManifestResourceStream(name))
        {
          digitImgs[rank] = new Bitmap(stream);
        }
      }
    }

    IAsyncBitmap image;
    Finder finder;
    Rectangle gridRect;
    int dim;
    double cellDim;

    Bitmap bitmap; //TODO DELETE

    public AndroidParser(IAsyncBitmap image, Bitmap bitmap)
    {
      this.image = image;
      this.finder = new Finder(image);
      this.bitmap = bitmap;
    }

    async Task FindDim()
    {
      var gridTLw = await finder.FindColor(gridRect.TopLeft(), Color.White, pp => pp.Right().Down());
      var next5 = await finder.FindColor(gridTLw, Color.Black, Dir.Right);
      next5 = await finder.FindColor(next5, Color.White, Dir.Right);
      await finder.Pulse(next5);

      var blockW = next5.X - gridTLw.X;
      dim = (int)Math.Round(5.0 * gridRect.Width / blockW);

      cellDim = ((double)gridRect.Width) / dim;

      Console.Error.WriteLine("DIM: " + dim);
    }

    async Task<List<Rectangle>> ScanDigits(Point p, int rightEnd)
    {
      var bwFinder = new Finder(new BWImage { IAsyncBitmap = image });

      var ans = new List<Rectangle>();
      while (true)
      {
        p = await bwFinder.FindColor(p, Color.Black, Dir.Right);
        await finder.Pulse(p);
        if (p.X > rightEnd) break;
        var rect = await bwFinder.FindBoundary(p);
        ans.Add(rect);
        p = rect.Right();
      }
      return ans;
    }

    public static double Score(Bitmap expected, Bitmap actual)
    {
      if (expected.Width != actual.Width || expected.Height != actual.Height) {
        throw new ArgumentException();
      }

      double diff = 0;
      for (var y = 0; y < expected.Height; ++y) {
        for (var x = 0; x < expected.Width; ++x) {
          diff += Math.Abs(expected.GetPixel(x, y).GetBrightness() - actual.GetPixel(x, y).GetBrightness());
        }
      }
      return diff;
    }

    int ParseDigits(List<Rectangle> rects)
    {
      var s = "";
      foreach (var r in rects)
      {
        //SaveToFile(r);
        var size = digitImgs.Values.First();

        var scores = new Dictionary<char, double>();
        using (var cropped = bitmap.Clone(r, System.Drawing.Imaging.PixelFormat.DontCare))
        using (var resized = new Bitmap(cropped, size.Width, size.Height)) {
          foreach (var (c, golden) in digitImgs)
          {
            scores[c] = Score(golden, resized);
          }
        }

        s += scores.OrderBy(kvp => kvp.Value).First().Key;
      }

      return int.Parse(s);
    }

    async Task<List<List<int>>> ParseTop()
    {
      List<List<int>> clues = new List<List<int>>();

      var p = gridRect.TopLeft().Up();
      p.X += (int)(cellDim / 2);
      for (var x = 0; x < dim; ++x)
      {
        p.X = gridRect.Left + (int)((x + 0.5) * cellDim);
        var clueP = await finder.FindColor(p.Up(), c => !c.ArgbEquals(Color.White), Dir.Up);
        var clueR = await finder.FindColor(clueP, Color.White, Dir.Right);

        clueP = await finder.FindColor(clueP, Color.White, Dir.Left);
        clueP.Y -= (int)(cellDim * 0.36);

        var col = new List<int>();
        while (true)
        {
          var rects = await ScanDigits(clueP, clueR.X);
          if (rects.Count == 0) break;
          col.Add(ParseDigits(rects));
          clueP.Y = rects.First().Middle().Y - 2 * rects.First().Height;
        }

        col.Reverse();
        clues.Add(col);

        Console.Error.WriteLine(string.Join(" ", col));
      }

      return clues;
    }

    async Task<List<List<int>>> ParseLeft()
    {
      List<List<int>> clues = new List<List<int>>();
      var gapSize = cellDim / 5.8;
      Console.Error.WriteLine("cellDim: " + cellDim);

      for (var y = 0; y < dim; ++y)
      {
        var p = new Point(0, gridRect.Top + (int)((y + 0.5) * cellDim));

        var prevRight = 0;
        var splitByGaps = new List<List<Rectangle>>();
        List<Rectangle> currList = null;
        foreach (var r in await ScanDigits(p, gridRect.Left - 1)) {
          if (r.Left - prevRight > gapSize) {
            currList = new List<Rectangle>();
            splitByGaps.Add(currList);
          }
          prevRight = r.Right;
          currList.Add(r);
        }

        clues.Add(splitByGaps.Select(ParseDigits).ToList());

        Console.Error.WriteLine(string.Join(" ", clues.Last()));
      }

      return clues;
    }

    public async Task<Puzzle> Parse()
    {
      var p = new Point(image.Width - 1, image.Height / 2);
      p = await finder.FindColor(p, Color.Black, Dir.Left);


      List<List<int>> top, left;
      using (var noDelay = new NoDelay(image))
      {
        gridRect = await finder.FindBoundary(p);
        await FindDim();
        top = await ParseTop();
        left = await ParseLeft();
      }

      return new Puzzle {
        Dim = dim,
        Vertical = top,
        Horizontal = left,
      };
    }

    public Point getCell(int x, int y) {
      if (gridRect == Rectangle.Empty) {
        throw new InvalidOperationException();
      }

      var opp = gridRect.TopLeft();
      // (0, 3) offset by cellDim * (1x, 7x)
      opp.X += (int)(cellDim * (2 * x + 1));
      opp.Y += (int)(cellDim * (2 * y + 1));

      return gridRect.TopLeft().Average(opp);
    }

    sealed class BWImage : DelgatingAsyncBitmap
    {
      public async override Task<Color> GetPixel(Point p)
      {
        if (p.X == Width - 1)
        {
          // Terrible hack to avoid scanning right off the end
          return Color.Black;
        }

        var c = await base.GetPixel(p);
        return c.GetBrightness() > 0.7 ? Color.White : Color.Black;
      }
    }

    public Task Pulse(Point p) {
      return finder.Pulse(p);
    }

    void SaveToFile(Rectangle r)
    {
      Bitmap cropped = new Bitmap(r.Width, r.Height);
      using (Graphics g = Graphics.FromImage(cropped))
      {
        g.DrawImage(bitmap, 0, 0, r, GraphicsUnit.Pixel);
        var path = @"img\" + Guid.NewGuid().ToString().Replace("-", "") + ".png";
        cropped.Save(path);
      }
    }

    async void TestBWFilter(Rectangle rect)
    {
      // var rect = new Rectangle(507, 600, 60, 23);
      SaveToFile(rect);
      var bw = new BWImage { IAsyncBitmap = image };
      for (int y = rect.Top; y < rect.Bottom; ++y)
      {
        var row = new List<float>();
        for (int x = rect.Left; x < rect.Right; ++x)
        {
          row.Add(bitmap.GetPixel(x, y).GetBrightness());
          bitmap.SetPixel(x, y, await bw.GetPixel(new Point(x, y)));
        }
        Console.Error.WriteLine(string.Join(' ', row.Select(f => f.ToString(".000"))));
      }
      SaveToFile(rect);

      Console.Error.WriteLine("BITMAP HAS BEEN MUTATED!");
    }
  }

  sealed class NoDelay : IDisposable
  {
    IAsyncBitmap image;
    object delay;

    public NoDelay(IAsyncBitmap im)
    {
      while (im.GetType().Name != "DelayedBitmap")
      {
        im = (IAsyncBitmap)im
          .GetType()
          .BaseType
          .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .First()
          .GetValue(im);
      }
      im.GetType().GetField(
          "count",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        ).SetValue(im, int.MinValue);

      image = im;
      delay = im.GetType().GetProperty("DelayCount").GetValue(im);
      im.GetType().GetProperty("DelayCount").SetValue(im, int.MaxValue / 2);
    }

    public void Dispose()
    {
      image.GetType().GetField(
        "count",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(image, int.MaxValue / 2);
      image.GetType().GetProperty("DelayCount").SetValue(image, delay);
    }
  }
}
