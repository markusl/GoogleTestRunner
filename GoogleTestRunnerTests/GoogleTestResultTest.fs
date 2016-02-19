namespace GoogleTestRunnerTests
open GoogleTestRunner
open System
open FsUnit
open Microsoft.VisualStudio.TestTools.UnitTesting
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open Microsoft.VisualStudio.TestPlatform.ObjectModel

type Logger() =
    interface IMessageLogger with
        override x.SendMessage(level, message) =
            printfn "%s" message

[<TestClass>]            
type ``GoogleTestResult reads results`` ()=
    let logger = new Logger()
    let sample1 = "SampleResult1.xml"
    let sample2 = "SampleResult2.xml"

    let doTestCase (a:string) (b:string) =
        let fn = sprintf "%s.%s" a b
        let tc = TestCase(fn, Uri("http://none"), "ff.exe")
        tc

    [<TestMethod>] member x.``finds successful result from sample1`` ()=
                    let result = ResultParser.getResults logger sample1 [doTestCase "GoogleTestSuiteName1" "TestMethod_001"]
                    result.Length |> should equal 1
                    result |> List.iter (fun f -> f.Outcome |> should equal TestOutcome.Passed)

    [<TestMethod>] member x.``finds successful parameterized result from sample1`` ()=
                    let results = ResultParser.getResults logger sample1 [doTestCase "ParameterizedTestsTest1/AllEnabledTest" "TestInstance/7  # GetParam() = (false, 200, 0)"]
                    results.Length |> should equal 1
                    let result = results.[0]
                    result.Outcome |> should equal TestOutcome.Passed
                    result.ErrorMessage |> should be Null

    [<TestMethod>] member x.``finds failure result sample1`` ()=
                    let results = ResultParser.getResults logger sample1 [doTestCase "AnimalsTest" "testGetEnoughAnimals"]
                    results.Length |> should equal 1
                    let result = results.[0]
                    result.Outcome |> should equal TestOutcome.Failed
                    result.ErrorMessage |> should not' (equal EmptyString)
                
    [<TestMethod>] member x.``finds parameterized failure result sample1`` ()=
                    let results = ResultParser.getResults logger sample1 [doTestCase "ParameterizedTestsTest1/AllEnabledTest" "TestInstance/11  # GetParam() = (true, 0, 100)"]
                    results.Length |> should equal 1
                    let result = results.[0]
                    result.Outcome |> should equal TestOutcome.Failed
                    result.ErrorMessage |> should equal ("""someSimpleParameterizedTest.cpp:61
Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0""".Replace("\r\n", "\n"))

    [<TestMethod>] member x.``finds type parameterized failure result sample1`` ()=
                    let results = ResultParser.getResults logger sample1 [doTestCase "SimpleTypeParameterizedTest/0" "SimpleTypedTest3"]
                    results.Length |> should equal 1
                    let result = results.[0]
                    result.Outcome |> should equal TestOutcome.Failed
                    result.ErrorMessage |> should equal ("""TypeParameterizedTests.cpp:256
Expected: TestFunctionX<class FirstType>() doesn't throw an exception.
  Actual: it throws.""".Replace("\r\n", "\n"))
                
    [<TestMethod>] member x.``finds type parameterized success result sample1`` ()=
                    let results = ResultParser.getResults logger sample1 [doTestCase "SimpleTypeParameterizedTest/1" "SimpleTypedTest0"]
                    results.Length |> should equal 1
                    let result = results.[0]
                    result.Outcome |> should equal TestOutcome.Passed
                    result.ErrorMessage |> should be Null
                
    [<TestMethod>] member x.``finds successful result from sample2`` ()=
                    let results = ResultParser.getResults logger sample2 [doTestCase "FooTest" "DoesXyz"]
                    results.Length |> should equal 1
                    let result = results.[0]
                    result.Outcome |> should equal TestOutcome.Passed
                    result.ErrorMessage |> should be Null

    [<TestMethod>] member x.``finds failure result sample2`` ()=
                    let result = ResultParser.getResults logger sample2 [doTestCase "FooTest" "MethodBarDoesAbc"]
                    result.Length |> should equal 1
                    let f = result.[0]
                    f.Outcome |> should equal TestOutcome.Failed
                    f.ErrorMessage |> should equal ("""c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp:40
Value of: output_filepath
  Actual: "this/package/testdata/myoutputfile.dat"
Expected: input_filepath
Which is: "this/package/testdata/myinputfile.dat"
Something's not right!!

c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp:41
Value of: 56456
Expected: 12312
Something's wrong :(""".Replace("\r\n", "\n"))
