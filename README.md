# AutoNonogram

A dotnet core [CLI `ANG`](ANG/) and WinForms [`GUI`](GUI/) that automates solving some [Nonogram](https://en.wikipedia.org/wiki/Nonogram) Android app.

The [`Parser`](Parser/) library converts images to a `Puzzle` object, which the [`Solver`](Solver/) solves by looking at independent rows and columns.

An [ADB client `Phone`](Phone/) takes screenshots `adb exec-out screencap` and rapidly taps. Instead of directly invoking `adb input tap` (the `input` command was taking about a second for each execution on the phone), [use MonkeyRunner Jython](https://stackoverflow.com/a/64635529/771768) for tapping. Sending messages to the Jython process using stdin/stdout [wasn't working](https://stackoverflow.com/a/64634081/771768), so communicate over HTTP requests with the [`MonkeyTapper` server](MonkeyTapper/).
