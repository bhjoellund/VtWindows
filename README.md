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
