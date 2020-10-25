using System;
using System.Diagnostics;

namespace Phone
{
    public static class Adb
    {
      public static void Tap(int x, int y) {
        var psi = new ProcessStartInfo
        {
          FileName = @"C:\Carl\scoop\shims\adb.exe",
          Arguments = $"shell input tap {x} {y}",
          UseShellExecute = false,
          // RedirectStandardOutput = true,
          // RedirectStandardError = true,
          CreateNoWindow = true,
        };
        using (var p = Process.Start(psi)) {
          p.WaitForExit();
          if (p.ExitCode != 0) {
            throw new InvalidOperationException(p.ExitCode.ToString());
          }
        }
      }
    }
}
