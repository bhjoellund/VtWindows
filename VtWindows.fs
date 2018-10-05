namespace VtWindows

open System
open System.Runtime.InteropServices

[<AutoOpen>]
module private Constants =
    let [<Literal>] STD_INPUT_HANDLE : int32 = -10
    let [<Literal>] STD_OUTPUT_HANDLE : int32 = -11
    let [<Literal>] ENABLE_VIRTUAL_TERMINAL_PROCESSING : uint32 = 0x0004u
    let [<Literal>] DISABLE_NEWLINE_AUTO_RETURN : uint32 = 0x0008u
    let [<Literal>] ENABLE_VIRTUAL_TERMINAL_INPUT : uint32 = 0x0200u

[<AutoOpen>]
module private WinApi =
    [<DllImport("kernel32.dll")>]
    extern bool GetConsoleMode(nativeint hConsoleHandle, uint32& lpMode)
    [<DllImport("kernel32.dll")>]
    extern bool SetConsoleMode(nativeint hConsoleHandle, uint32 dwMode)
    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern nativeint GetStdHandle(int32 nStdHandle)
    [<DllImport("kernel32.dll")>]
    extern uint32 GetLastError()

type VtWindowsError =
    | FailedGetMode
    | FailedSetMode

module VT =
    let (>>=) x f = Result.bind f x

    let private isWindows () =
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT
        | PlatformID.Win32S
        | PlatformID.Win32Windows -> true
        | _ -> false

    let private getInputModes stdIn stdOut : Result<uint32*uint32, _> =
        let mutable inMode : uint32 = 0u
        let mutable outMode : uint32 = 0u
        let inSuccess = GetConsoleMode(stdIn, &inMode)
        let outSuccess = GetConsoleMode(stdOut, &outMode)

        match (inSuccess && outSuccess) with
        | true -> Ok (inMode, outMode)
        | false -> Error FailedGetMode

    let private transform (inMode, outMode) =
        let inMode = inMode ||| ENABLE_VIRTUAL_TERMINAL_INPUT
        let outMode = outMode ||| ENABLE_VIRTUAL_TERMINAL_PROCESSING ||| DISABLE_NEWLINE_AUTO_RETURN
        Ok (inMode, outMode)

    let private setInputModes stdIn stdOut (inMode, outMode) =
        let inSuccess = SetConsoleMode(stdIn, inMode)
        let outSuccess = SetConsoleMode(stdOut, outMode)

        match (inSuccess && outSuccess) with
        | true -> Ok ()
        | false -> Error FailedSetMode

    let enable () =
        let stdIn = GetStdHandle(STD_INPUT_HANDLE)
        let stdOut = GetStdHandle(STD_OUTPUT_HANDLE)
        getInputModes stdIn stdOut
        >>= transform
        >>= setInputModes stdIn stdOut