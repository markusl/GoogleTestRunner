module GoogleTestCommandLineTest
open FsUnit
open NUnit.Framework
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open GoogleTestRunner

[<TestFixture>] 
type ``Test GoogleTestCommandLine`` () =
    let toTestCase (a:string)=
        let tc = TestCase(a, System.Uri("http://none"), "ff.exe")
        tc

    [<Test>] member x.``test arguments when running all tests`` () =
                GoogleTestCommandLine(true, List.Empty, List.Empty, "").GetCommandLine()
                    |> should equal "--gtest_output=\"xml:\" "

    [<Test>] member x.``combines common tests in suite`` () =
                let commonSuite = ["FooSuite.BarTest"; "FooSuite.BazTest"] |> List.map toTestCase
                GoogleTestCommandLine(false, commonSuite, commonSuite, "").GetCommandLine()
                    |> should equal "--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:"

    [<Test>] member x.``combines common tests in suite, in different order`` () =
                let commonSuite = ["FooSuite.BarTest"; "FooSuite.BazTest"; "FooSuite.gsdfgdfgsdfg"; "FooSuite.23453452345"; "FooSuite.bxcvbxcvbxcvb"] |> List.map toTestCase
                let commonSuiteBackwards = commonSuite |> List.rev
                GoogleTestCommandLine(false, commonSuite, commonSuiteBackwards, "").GetCommandLine()
                    |> should equal "--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:"
                GoogleTestCommandLine(false, commonSuiteBackwards, commonSuite, "").GetCommandLine()
                    |> should equal "--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:"

    [<Test>] member x.``does not combine cases not having common suite`` () =
                let cases = ["FooSuite.BarTest"; "BarSuite.BazTest1"] |> List.map toTestCase
                let allCases = ["FooSuite.BarTest"; "FooSuite.BazTest"; "BarSuite.BazTest1"; "BarSuite.BazTest2"] |> List.map toTestCase
                GoogleTestCommandLine(false, allCases, cases, "").GetCommandLine()
                    |> should equal "--gtest_output=\"xml:\" --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1"

    [<Test>] member x.``does not combine cases not having common suite, in different order`` () =
                let cases = ["BarSuite.BazTest1"; "FooSuite.BarTest"] |> List.map toTestCase
                let allCases = ["BarSuite.BazTest1"; "FooSuite.BarTest"; "FooSuite.BazTest"; "BarSuite.BazTest2"] |> List.map toTestCase
                GoogleTestCommandLine(false, allCases, cases, "").GetCommandLine()
                    |> should equal "--gtest_output=\"xml:\" --gtest_filter=BarSuite.BazTest1:FooSuite.BarTest"
                
