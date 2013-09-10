namespace GoogleTestRunner
open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel

type String() =
    static member JoinBy(str, mapping, elements) =
        String.Join(str, elements |> Seq.map mapping)

module extension =         
    type System.String with
        member x.AppendIfNotEmpty(str) =
            if String.IsNullOrWhiteSpace(x) then x
            else x + str

open extension

/// Purpose of this class is to create command line for executing tests in GoogleTest executable
/// 1. If we run all tests, do not specify filter
/// 2. If we run multiple tests from same suite, check if these can be combined into one parameter
///    "FooSuite.BarTest"; "FooSuite.BazTest" -> "FooSuite.*"
/// 3. Rests of the tests are listed individually
type GoogleTestCommandLine(runAll, allCases:TestCase list, cases, outputPath) =
    let fqn (c:TestCase) = c.FullyQualifiedName
    let testSuiteNameFromCase case = (case |> fqn).Split([|'.'|]).[0]
    let allMatchingCases cases suite =
        cases |> List.filter(fun case -> (case |> fqn).StartsWith(suite))
    let differentSuites =
        cases
            |> Seq.groupBy testSuiteNameFromCase
            |> List.ofSeq
            |> List.map fst
    let suitesRunningAllTests =
        differentSuites |> List.filter(fun suite ->
            let allToBeRun = allMatchingCases cases suite
            let allReally = allMatchingCases allCases suite
            allToBeRun.Length = allReally.Length // Length check is enough
        )
    let filterForSuitesRunningAllTests =
        String.Join(".*:", suitesRunningAllTests).AppendIfNotEmpty(".*:")
    let filterForSuitesRunningIndividualTests =
        let casesNotHavingCommonSuite =
            cases |> List.filter(fun case ->
                        not(suitesRunningAllTests |> List.exists (fun i -> i = (case |> testSuiteNameFromCase))))
        String.JoinBy(":", fqn, casesNotHavingCommonSuite)
    let filter =
        if runAll then ""
        else sprintf "--gtest_filter=%s%s" filterForSuitesRunningAllTests filterForSuitesRunningIndividualTests
    member x.GetCommandLine() = (sprintf "--gtest_output=xml:%s %s" outputPath filter)
