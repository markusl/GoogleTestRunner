module GoogleTestDiscovererTest
open GoogleTestRunner
open System
open FsUnit
open NUnit.Framework
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open Microsoft.VisualStudio.TestPlatform.ObjectModel

let x86staticallyLinkedTests = @"..\..\..\data\x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe"
let x86externallyLinkedTests = @"..\..\..\data\x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe"
let x86crashingTests = @"..\..\..\data\x86\CrashingGoogleTests\CrashingGoogleTests.exe"
let x64staticallyLinkedTests = @"..\..\..\data\x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe"
let x64externallyLinkedTests = @"..\..\..\data\x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe"
let x64crashingTests = @"..\..\..\data\x64\CrashingGoogleTests\CrashingGoogleTests.exe"

let gtestBasicMethods = """GoogleTestSuiteName1.
  TestMethod_001
  TestMethod_002
  TestMethod_003
  TestMethod_004
  TestMethod_005
  TestMethod_006
  TestMethod_007
SecondGoogleTestSuiteName.
  FirstTestMethodName
  SecondSomething
"""

let gtest170TypedMethods = """
DisabledTestsTest.
  DISABLED_TestShouldNotRun_1
  DISABLED_TestShouldNotRun_2
TypedTest/0.  # TypeParam = int
  DISABLED_ShouldNotRun
TypedTest/1.  # TypeParam = double
  DISABLED_ShouldNotRun
DISABLED_TypedTest/0.  # TypeParam = int
  ShouldNotRun
DISABLED_TypedTest/1.  # TypeParam = double
  ShouldNotRun
My/TypedTestP/0.  # TypeParam = int
  DISABLED_ShouldNotRun
My/TypedTestP/1.  # TypeParam = double
  DISABLED_ShouldNotRun
My/DISABLED_TypedTestP/0.  # TypeParam = int
  ShouldNotRun
My/DISABLED_TypedTestP/1.  # TypeParam = double
  ShouldNotRun
"""
type Logger() =
    interface IMessageLogger with
        override x.SendMessage(level, message) =
            printfn "%s" message

[<TestFixture>] 
type ``GoogleTestDiscoverer`` ()=
    let doTestCase (a:string) (b:string) =
        let fn = sprintf "%s.%s" a b
        let tc = TestCase(fn, Uri("http://none"), "ff.exe")
        tc
        
    [<Test>] member x.``matches test executable name`` () =
                let isGtest = DiscovererUtils.isGoogleTestExecutable (Logger())
                isGtest "MyGoogleTests.exe" |> should be True
                isGtest "MyGoogleTest.exe" |> should be True
                isGtest "mygoogletests.exe" |> should be True
                isGtest "mygoogletest.exe" |> should be True
                isGtest "MyGoogleTes.exe" |> should be False
                isGtest "TotallyWrong.exe" |> should be False
                isGtest "TestStuff.exe" |> should be False
                isGtest "TestLibrary.exe" |> should be False
    
    [<Test>] member x.``parses test case list`` () =
                let result = DiscovererUtils.parseTestCases (gtestBasicMethods.Split([|'\n'|]) |> List.ofArray)
                result.Length |> should equal 9
                result.[0] |> should equal ("SecondGoogleTestSuiteName", "SecondSomething")
                result.[1] |> should equal ("SecondGoogleTestSuiteName", "FirstTestMethodName")
                result.[2] |> should equal ("GoogleTestSuiteName1", "TestMethod_007")
                result.[3] |> should equal ("GoogleTestSuiteName1", "TestMethod_006")
                result.[4] |> should equal ("GoogleTestSuiteName1", "TestMethod_005")

    [<Test>] member x.``parses test case list from googletest 1.7.0 output`` () =
                let result = DiscovererUtils.parseTestCases (gtest170TypedMethods.Split([|'\n'|]) |> List.ofArray)
                result.Length |> should equal 10
                result.[0] |> should equal ("My/DISABLED_TypedTestP/1", "ShouldNotRun")
                result.[1] |> should equal ("My/DISABLED_TypedTestP/0", "ShouldNotRun")
                result.[2] |> should equal ("My/TypedTestP/1", "DISABLED_ShouldNotRun")
                result.[3] |> should equal ("My/TypedTestP/0", "DISABLED_ShouldNotRun")
                result.[4] |> should equal ("DISABLED_TypedTest/1", "ShouldNotRun")
                
    member x.``finds tests from statically linked executable with source file locations`` (location) =
                let tests = DiscovererUtils.getTestsFromExecutable (Logger()) location
                tests.Length |> should equal 2
                tests.[0].DisplayName |> should equal "FooTest.DoesXyz"
                tests.[1].DisplayName |> should equal "FooTest.MethodBarDoesAbc"
                tests.[0].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp"
                tests.[1].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp"
                tests.[0].LineNumber |> should equal 45
                tests.[1].LineNumber |> should equal 36
                
    [<Test>] member x.``finds tests from statically linked x86 executable with source file locations`` () =
                x.``finds tests from statically linked executable with source file locations`` x86staticallyLinkedTests

    [<Test>] member x.``finds tests from statically linked x64 executable with source file locations`` () =
                x.``finds tests from statically linked executable with source file locations`` x64staticallyLinkedTests
                
    member x.``finds tests from externally linked executable with source file locations`` (location) =
                let tests = DiscovererUtils.getTestsFromExecutable (Logger()) location
                tests.Length |> should equal 2
                tests.[0].DisplayName |> should equal "BarTest.DoesXyz"
                tests.[1].DisplayName |> should equal "BarTest.MethodBarDoesAbc"
                tests.[0].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp"
                tests.[1].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp"
                tests.[0].LineNumber |> should equal 44
                tests.[1].LineNumber |> should equal 36
                
    [<Test>] member x.``finds tests from externally linked x86 executable with source file locations`` () =
                x.``finds tests from externally linked executable with source file locations`` x86externallyLinkedTests

    [<Test>] member x.``finds tests from externally linked x64 executable with source file locations`` () =
                x.``finds tests from externally linked executable with source file locations`` x64externallyLinkedTests
