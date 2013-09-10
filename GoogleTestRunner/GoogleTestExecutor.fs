namespace GoogleTestRunner

open System
open System.Diagnostics
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

[<ExtensionUri(Constants.identifierUri)>]
type GoogleTestExecutor() =
    let mutable cancelled = false

    let runOnce (framework : IFrameworkHandle) (runContext : IRunContext) allCases cases executable runAll = 
        let outputPath = IO.Path.GetTempFileName()
        let arguments = GoogleTestCommandLine(runAll, allCases, cases, outputPath).GetCommandLine()
        
        let wd = IO.Path.GetDirectoryName executable
        cases |> List.iter framework.RecordStart
        if runContext.IsBeingDebugged then
            framework.SendMessage(TestMessageLevel.Informational, "Attaching debugger to " + executable)
            Process.GetProcessById(framework.LaunchProcessWithDebuggerAttached(executable, wd, arguments, null)).WaitForExit();
        else
            framework.SendMessage(TestMessageLevel.Informational, sprintf "In %s, running: %s  %s" wd executable arguments)
            ProcessUtil.runCommand wd executable arguments |> ignore
        let results = ResultParser.getResults framework outputPath cases
        results |> List.iter framework.RecordResult

    let runTests allCases (cases : IEnumerable<TestCase>) (runContext : IRunContext) (framework : IFrameworkHandle) runAll =
        let cases = cases |> Seq.groupBy(fun c -> c.Source) |> List.ofSeq
        for executable, cases in cases do
            if not(cancelled) then
                try
                    runOnce framework runContext allCases (cases |> List.ofSeq) executable runAll
                with e ->
                    framework.SendMessage(TestMessageLevel.Error, e.Message)
                    framework.SendMessage(TestMessageLevel.Error, e.StackTrace)

    interface ITestExecutor with
        override x.Cancel() =
            cancelled <- true

        override x.RunTests(tests : IEnumerable<TestCase>, runContext : IRunContext, framework : IFrameworkHandle) =
            cancelled <- false
            let allTestCasesInAllExecutables = 
                tests
                |> Seq.map(fun f -> f.Source)
                |> Seq.distinct
                |> List.ofSeq
                |> List.map (fun f -> DiscovererUtils.getTestsFromExecutable framework f |> List.ofArray)
                |> List.concat
            runTests allTestCasesInAllExecutables tests runContext framework false

        override x.RunTests(tests : IEnumerable<string>, runContext : IRunContext, framework : IFrameworkHandle) =
            cancelled <- false
            for executable in tests do
                if not(cancelled) then
                    let allCases = DiscovererUtils.getTestsFromExecutable framework executable |> List.ofSeq
                    runTests allCases allCases runContext framework true
