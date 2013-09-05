namespace GoogleTestRunner
open System
open System.IO
open System.Diagnostics
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

type ProcessUtil() =
    static member readTheStream (stream : StreamReader) (ret : Process) = 
        let rec readTheStreamInner (stream : StreamReader) (ret : Process) streamContent =
            if stream.EndOfStream && ret.HasExited then streamContent
            else readTheStreamInner stream ret (stream.ReadLine() :: streamContent)
        List.rev(readTheStreamInner stream ret [])

    static member getOutputOfCommand wd command param =
        use ret = Process.Start(ProcessStartInfo(command, param,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = false,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    WorkingDirectory = wd))
        (ret.WaitForInputIdle, ret.BeginOutputReadLine) |> ignore
        ProcessUtil.readTheStream ret.StandardOutput ret

    static member runCommand wd command param =
        ignore (ProcessUtil.getOutputOfCommand wd command param)