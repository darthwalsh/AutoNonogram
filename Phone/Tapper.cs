using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Phone
{
  public class Tapper : IDisposable
  {
    Process process;
    bool ready;

    public Tapper()
    {
      var psi = new ProcessStartInfo
      {
        FileName = @"C:\Users\cwalsh\scoop\apps\android-sdk\current\tools\bin\monkeyrunner.bat",
        Arguments = @"C:\code\AutoNonogram\MonkeyTapper\tapper.py",

        UseShellExecute = true,
      };
      process = Process.Start(psi);
    }

    public void Dispose()
    {
      // TODO doesn't work to actually kill java process
      // process?.Kill();
    }

    public void Tap(IEnumerable<(int x, int y)> points)
    {
      while (!ready) {
        try {
          var get = (HttpWebRequest)WebRequest.Create("http://localhost:8080/");
          using (get.GetResponse()) { }
          ready = true;
        } catch (WebException ex) {
          Console.Error.Write(ex.Message);
          Thread.Sleep(100);
        }
      }

      var body = string.Join(";", points.Select(p => $"{p.x},{p.y}"));
      var data = Encoding.ASCII.GetBytes(body);
      
      var request = (HttpWebRequest)WebRequest.Create("http://localhost:8080/");
      request.Method = "POST";
      request.ContentType = "application/octet-stream";
      request.ContentLength = data.Length;

      using (var stream = request.GetRequestStream())
      {
        stream.Write(data, 0, data.Length);
      }

      using (request.GetResponse()) { }
    }
  }
}
