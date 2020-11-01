# Helper app to tap really fast

## Install

Install android tools ADB, SDK, and old JDK8.

With scoop, use these packages and buckets:

```
  adb 30.0.4 [main]
  adopt8-upstream 8u272-b10 [java]
  android-sdk 4333796 [extras]
```

Overwrite wrong paths in monkeyrunner.bat: https://stackoverflow.com/questions/44168880/android-sdk-monkeyrunner-25-3-2-wont-run

## Set up environment

Run `scoop reset adopt8-upstream`

Ensure that `java -version` prints version 1.8.

When you pass a script file argument to `monkeyrunner.bat`, use the full path (not relative).
