[![Build status](https://ci.appveyor.com/api/projects/status/krprf88vw8s51ii3?svg=true)](https://ci.appveyor.com/project/mofibrian/vtwindows)

# VtWindows

Just a very simple repository to enable using Virtual Terminal (VT) sequences in a Windows command-line application

# Usage
You can use `VtWindows.fs` as a file in your own project or you can use it as a referenced project
Whichever you prefer, the way to use it is very simple

```fsharp
open VtWindows
// ...
match VT.enable () with
| Ok _ -> // VT sequences were successfully enabled
| Error e -> eprintfn "%A" e // VT sequences could not be enabled
```
