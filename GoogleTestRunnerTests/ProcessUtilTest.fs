namespace ProcessUtilTest

open FsUnit
open GoogleTestRunner
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type ``Test ProcessUtil`` () =
                
    [<TestMethod>]
    member x.``returns output of command`` () =
        ProcessUtil.getOutputOfCommand(".", "cmd.exe", "/C \"echo 2\"") |> should equal ["2"]
        
    [<TestMethod>]
    member x.``throws if process fails`` () =
        (fun () -> ProcessUtil.getOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", true) |> ignore)
            |> should throw typeof<System.Exception>

    [<TestMethod>]
    member x.``does not throw if process fails`` () =
        ProcessUtil.getOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"") |> should equal List<string>.Empty
                