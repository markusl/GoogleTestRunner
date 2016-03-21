namespace GoogleTestRunner
open System
open System.Text.RegularExpressions
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

module Constants =
    [<Literal>]
    let identifierUri = "executor://GoogleTestRunner/v1";
    let gtestListTests = "--gtest_list_tests"
    let gtestTestBodySignature = "::TestBody"

module DiscovererUtils =
    let isGoogleTestExecutable (logger:IMessageLogger) e =
        let executablesAllowed = "[Tt]est[s]{0,1}.*.exe"
        let matches = Regex.IsMatch(e, executablesAllowed)
        logger.SendMessage(TestMessageLevel.Informational,
            sprintf "GoogleTest: Does %s match %s: %b" e executablesAllowed matches)
        matches

    /// Parses test case names from --gtest_list_tests output. Returns list of (testSuite, testCase)
    let parseTestCases (tests:string list) =
        let parseSingleTest (currentSuite, result) (currentLine:string) =
            let currentLine = currentLine.Trim([|'.'; '\n'; '\r'|])
            if currentLine.StartsWith("  ") then
                let caseName = currentLine.Split([|' '|], StringSplitOptions.RemoveEmptyEntries).[0]
                currentSuite, (currentSuite, caseName) :: result
            else
                let stripGtest170TypeParameter =
                    let split = currentLine.Split([|".  # TypeParam"|], StringSplitOptions.RemoveEmptyEntries)
                    if split.Length > 0 then split.[0] else currentLine
                stripGtest170TypeParameter, result
        snd (tests |> List.fold parseSingleTest ("", List.empty)) |> Array.ofList
        
    /// Gets the GoogleTest function name format!
    let googleTestCombinedName (testSuite: string, testMethod: string) = 
        let testSuiteName = testSuite.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries) :> seq<string> |> Seq.last
        let testMethodName = testMethod.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries).[0]
        sprintf "%s_%s_Test%s" testSuiteName testMethodName Constants.gtestTestBodySignature

    let getSourceFileLocations executable logger (testcases:(string * string) []) =
        let symbols = testcases |> Array.Parallel.map googleTestCombinedName
        let symbolFilterString = sprintf "*%s" Constants.gtestTestBodySignature
        DiaResolver.resolveAllMethods executable symbols symbolFilterString logger

    let toTestCase executable logger symbols (testSuite, testMethod) =
        let dn = sprintf "%s.%s" testSuite testMethod
        let sourceInfo =
            let cn = googleTestCombinedName (testSuite, testMethod)
            // The test methods may be defined inside (unnamed) namespace, so we won't
            // match exact string, but only a part of it. Avoid creating duplicate test suite 
            // and test method pairs :-)
            match symbols |> Array.tryFind(fun ((f:string), (a, b)) -> f.Contains(cn)) with
            | None -> sprintf "Couldn't locate %s" cn, -12
            | Some(info) -> snd info
        let category = testSuite.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries) :> seq<string> |> Seq.last
        let testCase = 
            TestCase(
                dn,
                Uri(Constants.identifierUri),
                executable,
                DisplayName = dn,
                CodeFilePath = fst sourceInfo,
                LineNumber = snd sourceInfo)
        testCase.Traits.Add(Trait("Category", category))
        testCase

    let getTestsFromExecutable (logger:IMessageLogger) executable =
        let gtestTestList = ProcessUtil.getOutputOfCommand(String.Empty, executable, Constants.gtestListTests, true)
        let testCases = parseTestCases gtestTestList
        logger.SendMessage(TestMessageLevel.Informational, sprintf "Found %d tests, resolving symbols" (testCases.Length))
        let symbols = getSourceFileLocations executable logger testCases
        testCases |> Array.Parallel.map (toTestCase executable logger symbols)

[<DefaultExecutorUri(Constants.identifierUri)>]
[<FileExtension(".exe")>]
type GoogleTestDiscoverer() =
    interface ITestDiscoverer with
        override x.DiscoverTests(executables, discoveryContext, logger, discoverySink) =
            let googleTestExecutables = executables |> Seq.filter (DiscovererUtils.isGoogleTestExecutable logger)
            let sinkAllTestCases logger (discoverySink:ITestCaseDiscoverySink) executable =
                let googleTestTests = DiscovererUtils.getTestsFromExecutable logger executable
                googleTestTests |> Array.iter discoverySink.SendTestCase
            googleTestExecutables |> Seq.iter (sinkAllTestCases logger discoverySink)
