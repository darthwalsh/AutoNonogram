using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ImageParse;

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

    async Task<List<int>> ParseDigits(List<Rectangle> rects)
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

      return rects.Any() ? new List<int> { int.Parse(s) } : new List<int>();
    }

    async Task<List<List<int>>> ParseTop()
    {
      List<List<int>> topClues = new List<List<int>>();
      double cellDim = ((double)gridRect.Width) / dim;

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
          var parsed = await ParseDigits(rects);
          col.Add(parsed.Single());
          clueP.Y = (int)(rects.First().Middle().Y - 0.7 * cellDim);
        }

        col.Reverse();
        topClues.Add(col);

        Console.Error.WriteLine(string.Join(", ", col));
      }

      return topClues;
    }

    public async Task<string> Parse()
    {
      var p = new Point(image.Width - 1, image.Height / 2);
      p = await finder.FindColor(p, Color.Black, Dir.Left);

      using (new NoDelay(image))
      {
        gridRect = await finder.FindBoundary(p);
      }

      await FindDim();

      var top = await ParseTop();

      return "TODO";
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
        return c.GetBrightness() > 0.5 ? Color.White : Color.Black;
      }
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
