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
    | UnsupportedOs

type Transform =
    | Enable
    | Disable

module VT =
    let (>>=) x f = Result.bind f x

    let isWindows () =
        match Environment.OSVersion.Platform with
        | PlatformID.Win32NT
        | PlatformID.Win32S
        | PlatformID.Win32Windows -> Ok ()
        | _ -> Error UnsupportedOs

    let private getHandles () =
        let stdIn = GetStdHandle(STD_INPUT_HANDLE)
        let stdOut = GetStdHandle(STD_OUTPUT_HANDLE)
        Ok (stdIn, stdOut)

    let private getInputModes (stdIn, stdOut) : Result<_, _> =
        let mutable inMode : uint32 = 0u
        let mutable outMode : uint32 = 0u
        let inSuccess = GetConsoleMode(stdIn, &inMode)
        let outSuccess = GetConsoleMode(stdOut, &outMode)

        match (inSuccess && outSuccess) with
        | true -> Ok (stdIn, stdOut, inMode, outMode)
        | false -> Error FailedGetMode

    let private disableBits bits x =
        if x &&& bits = bits
        then x ^^^ bits
        else x

    let private transform t (stdIn, stdOut, inMode, outMode) =
        match t with
        | Enable ->
            let inMode = inMode ||| ENABLE_VIRTUAL_TERMINAL_INPUT
            let outMode = outMode ||| ENABLE_VIRTUAL_TERMINAL_PROCESSING ||| DISABLE_NEWLINE_AUTO_RETURN
            Ok (stdIn, stdOut, inMode, outMode)
        | Disable ->
            let inMode = disableBits ENABLE_VIRTUAL_TERMINAL_INPUT inMode
            let outMode = disableBits outMode ENABLE_VIRTUAL_TERMINAL_PROCESSING
                          |> disableBits DISABLE_NEWLINE_AUTO_RETURN
            Ok (stdIn, stdOut, inMode, outMode)

    let private setInputModes (stdIn, stdOut, inMode, outMode) =
        let inSuccess = SetConsoleMode(stdIn, inMode)
        let outSuccess = SetConsoleMode(stdOut, outMode)

        match (inSuccess && outSuccess) with
        | true -> Ok ()
        | false -> Error FailedSetMode

    let private toggle t =
        isWindows ()
        >>= getHandles
        >>= getInputModes
        >>= transform t
        >>= setInputModes

    let enable () =
        toggle Enable

    let disable () =
        toggle Disable

