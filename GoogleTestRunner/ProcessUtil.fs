namespace GoogleTestRunner
open System
open System.IO
open System.Diagnostics
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

type ProcessUtil() =
    static member readTheStream throwIfNonZeroExitCode (stream : StreamReader) (ret : Process) = 
        let rec readTheStreamInner (stream : StreamReader) (ret : Process) streamContent =
            if stream.EndOfStream && ret.HasExited then
                if throwIfNonZeroExitCode && ret.ExitCode <> 0 then
                    failwith <| (sprintf "Process exited with code %d" ret.ExitCode)
                streamContent
            else readTheStreamInner stream ret (stream.ReadLine() :: streamContent)
        List.rev(readTheStreamInner stream ret [])

    /// Execute a command
    /// wd          Working directory
    /// commmand    Command to execute (start process, not shellexecute)
    /// param       Command arguments
    /// throw       Throw an error if process exit code is not zero (defaults to false)
    /// Returns the process output in a list of strings.
    static member getOutputOfCommand(wd, command, param, ?throwIfNonZeroExitCode0) =
        let throwIfNonZeroExitCode = defaultArg throwIfNonZeroExitCode0 false
        use ret = Process.Start(ProcessStartInfo(command, param,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = false,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    WorkingDirectory = wd))
        (ret.WaitForInputIdle, ret.BeginOutputReadLine) |> ignore
        ProcessUtil.readTheStream throwIfNonZeroExitCode ret.StandardOutput ret

    static member runCommand wd command param =
        ignore (ProcessUtil.getOutputOfCommand(wd, command, param))
