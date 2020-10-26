using System;
using System.Diagnostics;
using System.IO;

namespace Phone
{
  public static class Adb
  {
    public static void Tap(int x, int y)
    {
      var psi = new ProcessStartInfo
      {
        FileName = @"C:\Carl\scoop\shims\adb.exe",
        Arguments = $"shell input tap {x} {y}",
        UseShellExecute = false,
        // RedirectStandardError = true,
        CreateNoWindow = true,
      };
      using (var p = Process.Start(psi))
      {
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
          throw new InvalidOperationException(p.ExitCode.ToString());
        }
      }
    }

    public static MemoryStream Screenshot()
    {
      var psi = new ProcessStartInfo
      {
        FileName = @"C:\Carl\scoop\shims\adb.exe",
        Arguments = $"exec-out screencap -p",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true,
      };
      using (var p = Process.Start(psi))
      {
        var s = new MemoryStream();
        p.StandardOutput.BaseStream.CopyTo(s);
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
          throw new InvalidOperationException(p.ExitCode.ToString());
        }
        s.Position = 0;
        return s;
      }
    }
  }
}
