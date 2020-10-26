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
    static Dictionary<char, float[,]> digitImgs;

    static AndroidParser()
    {
      digitImgs = new Dictionary<char, float[,]>();
      var assembly = System.Reflection.Assembly.GetExecutingAssembly();
      foreach (var name in assembly.GetManifestResourceNames())
      {
        using (var stream = assembly.GetManifestResourceStream(name))
        using (var bitmap = new Bitmap(stream))
        {
          var pixels = new float[bitmap.Width, bitmap.Height];
          for (var y = 0; y < bitmap.Height; ++y)
          {
            for (var x = 0; x < bitmap.Width; ++x)
            {
              pixels[x, y] = bitmap.GetPixel(x, y).GetBrightness();
            }
          }
          var rank = name.Replace("Parser.img.", "").Replace(".png", "").Single();
          digitImgs[rank] = pixels;
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

    void SaveToFile(Rectangle r) {
      Bitmap cropped = new Bitmap(r.Width, r.Height);
      using (Graphics g = Graphics.FromImage(cropped)) {
        g.DrawImage(bitmap, 0, 0, r, GraphicsUnit.Pixel);
        var path = @"img\" + Guid.NewGuid().ToString().Replace("-", "") + ".png";
        cropped.Save(path);
      }
    }

    double Score(float[,] expected, float[,] actual) {
      throw new NotImplementedException();
    }

    async Task<List<int>> ParseDigits(List<Rectangle> rects)
    {
      foreach (var r in rects)
      {
        //SaveToFile(r);
        var pixels = new float[r.Width, r.Height];
        for (var y = 0; y < r.Height; ++y)
        {
          for (var x = 0; x < r.Width; ++x)
          {
            pixels[x, y] = (await image.GetPixel(new Point(x + r.Left, y + r.Top))).GetBrightness();
          }

          var scores = new Dictionary<char, double>();
          var s = new KeyValuePair<int, int>(1, 3);
          foreach (var (c, floats) in digitImgs) {
            scores[c] = Score(floats, pixels);
          }
        }
      }

      return rects.Any() ? new List<int> { 1 } : new List<int>();
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

        topClues.Add(col);
      }

      return topClues;
    }

    public async Task<string> Parse()
    {
      var p = new Point(image.Width - 1, image.Height / 2);
      p = await finder.FindColor(p, Color.Black, Dir.Left);

      using (new NoDelay(image)) {
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
