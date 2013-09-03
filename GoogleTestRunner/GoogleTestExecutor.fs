namespace GoogleTestRunner

open System
open System.Diagnostics
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

type String() =
    static member JoinBy(str, elements, mapping) =
        String.Join(str, elements |> Seq.map mapping)

[<ExtensionUri(Constants.identifierUri)>]
type GoogleTestExecutor() =
    let mutable cancelled = false

    let runOnce (framework : IFrameworkHandle) debug cases executable runAll = 
        let outputPath = IO.Path.GetTempFileName()
        let filter =
            let fqn (c:TestCase) = c.FullyQualifiedName
            if runAll then ""
            else sprintf "--gtest_filter=%s" (String.JoinBy(":", cases, fqn))
        let arguments = (sprintf "--gtest_output=xml:%s %s" outputPath filter)

        cases |> List.iter framework.RecordStart
        if debug then
            framework.SendMessage(TestMessageLevel.Informational, "Attaching debugger to " + executable)
            Process.GetProcessById(framework.LaunchProcessWithDebuggerAttached(executable, Environment.CurrentDirectory, arguments, null)).WaitForExit();
        else
            framework.SendMessage(TestMessageLevel.Informational, sprintf "Running: %s %s" executable arguments)
            ProcessUtil.runCommand executable arguments |> ignore
        let results = ResultParser.getResults framework outputPath cases
        results |> List.iter framework.RecordResult

    let runTests (tests : IEnumerable<TestCase>) (runContext : IRunContext) (framework : IFrameworkHandle) runAll =
        let cases = tests |> Seq.groupBy(fun c -> c.Source) |> List.ofSeq
        for executable, cases in cases do
            if not(cancelled) then
                try
                    runOnce framework runContext.IsBeingDebugged (cases |> List.ofSeq) executable runAll
                with e ->
                    framework.SendMessage(TestMessageLevel.Error, e.Message)
                    framework.SendMessage(TestMessageLevel.Error, e.StackTrace)

    interface ITestExecutor with
        override x.Cancel() =
            cancelled <- true

        override x.RunTests(tests : IEnumerable<TestCase>, runContext : IRunContext, framework : IFrameworkHandle) =
            cancelled <- false
            runTests tests runContext framework false

        override x.RunTests(tests : IEnumerable<string>, runContext : IRunContext, framework : IFrameworkHandle) =
            cancelled <- false
            for executable in tests do
                if not(cancelled) then
                    let allCases = DiscovererUtils.getTestsFromExecutable framework executable
                    runTests allCases runContext framework true

