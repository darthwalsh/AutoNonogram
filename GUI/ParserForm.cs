using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImageParse;
using Parser;
using Phone;

namespace GUI
{
  public partial class ParserForm : Form
  {
    PictureBox pictureBox;
    public ParserForm()
    {
      InitializeComponent();
      pictureBox = new PictureBox();
      this.Controls.Add(pictureBox);
      this.Load += new System.EventHandler(this.ParserForm_Load);
    }
    async private void ParserForm_Load(object sender, EventArgs e)
    {
      const int fps = 1000 / 60;

      var pngPath = Path.Combine(Application.StartupPath, "screen.png");
      Bitmap image;
      if (File.Exists(pngPath)) {
        image = new Bitmap(pngPath);
      } else {
        var screen = Adb.Screenshot();
        File.WriteAllBytes(pngPath, screen.GetBuffer());
        image = new Bitmap(screen);
      }

      var tallest = Screen.AllScreens.OrderByDescending(s => s.WorkingArea.Height).First();
      Location = tallest.WorkingArea.Location;
      
      pictureBox.Image = image;
      pictureBox.Height = tallest.WorkingArea.Height;
      pictureBox.Width = image.Width;

      Top = tallest.WorkingArea.Top;
      Size = pictureBox.Size;

      var todo = new Bitmap(image);

      using (var timer = new Timer { Interval = fps })
      {
        timer.Tick += (_, __) => 
          pictureBox.Refresh();
        timer.Start();

        var parser = new AndroidParser(new InvertingTrackingBitmap
        {
          KeepCount = 2000,
          IAsyncBitmap = new DelayedBitmap
          {
            DelayInterval = fps,
            DelayCount = 200,
            IAsyncBitmap = new WrappingBitmap
            {
              Bitmap = image,
            },
          },
        }, todo);
        var st = await parser.Parse();
        pictureBox.Refresh();
      }
    }
  }
}
