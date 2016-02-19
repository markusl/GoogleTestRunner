namespace GoogleTestRunner
open System
open System.IO
open FSharp.Data
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
type GoogleTestResult = XmlProvider<"..\data\SampleResult1.xml", Global=true>

/// For reading results from Xml
module ResultParser =
    let private xmlNotFound = "Output file does not exist, did your tests crash?"

    let inline isNull< ^a when ^a : not struct> (x:^a) =
        obj.ReferenceEquals (x, Unchecked.defaultof<_>)

    /// Maps given test cases to GoogleTest XML format results loaded from the specific file
    let getResults (logger:IMessageLogger) outputPath testCases =
        // We don't get output if the test runner crashes before it's written -> mark all tests skipped
        if not (File.Exists(outputPath)) then
            logger.SendMessage(TestMessageLevel.Warning, xmlNotFound)
            testCases |> List.map(fun tc ->
                TestResult(tc,
                        ComputerName = System.Environment.MachineName,
                        Outcome = TestOutcome.Skipped,
                        ErrorMessage = xmlNotFound
                    )
            )
        else
            let result = GoogleTestResult.Parse(File.ReadAllText(outputPath))
            logger.SendMessage(TestMessageLevel.Informational, "Opened results from " + outputPath)

            let testCaseResultsFlattened = result.Testsuites |> Array.collect (fun f -> f.Testcases |> Array.map(fun result ->
                (sprintf "%s.%s" result.Classname result.Name
                ,
                    (if result.Status.Equals("run") && not(result.XElement.HasElements) then TestOutcome.Passed
                        else if result.Status.Equals("run") && result.XElement.HasElements then TestOutcome.Failed
                        else if result.Status.Equals("notrun") then TestOutcome.Skipped
                        else TestOutcome.None)
                , result.TypeParam
                ,
                    (if result.Status.Equals("run") && result.XElement.HasElements then
                            String.Join("\n\n", result.Failures |> Array.map (fun f -> f.Value))
                        else null),
                        result.Time)))

            let mapTestCaseToResult (tc:TestCase) =
                match testCaseResultsFlattened |> Array.tryFind(fun (testMethod, _, _, _, _) -> tc.FullyQualifiedName.Split(' ').[0] = testMethod) with
                | Some(testMethod, result, typeparam, error, time) ->
                    TestResult(tc,
                        ComputerName = System.Environment.MachineName,
                        Outcome = result,
                        ErrorMessage = match error with 
                                            | null -> null
                                            | x ->  match typeparam with 
                                                       | Some x -> error.Replace("<TypeParam>", "<" + typeparam.Value.ToString() + ">")
                                                       | None -> error
                        ,
                        Duration = System.TimeSpan.FromMilliseconds(float (time * decimal 1000))
                    )
                | None ->
                    logger.SendMessage(TestMessageLevel.Error, sprintf "Couldn't find result for %s" (tc.FullyQualifiedName))
                    TestResult(tc)

            testCases |> Array.ofSeq |> Array.Parallel.map mapTestCaseToResult |> List.ofArray