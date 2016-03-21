module GoogleTestDiscovererTest
open GoogleTestRunner
open System
open FsUnit
open Microsoft.VisualStudio.TestTools.UnitTesting
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

let gtest170ParameterizedMethods = """ParameterizedTestsTest1/AllEnabledTest.
  /0  # GetParam() = (false, 0, -100)
  TestInstance/1  # GetParam() = (false, 0, 0)
  TestInstance/2  # GetParam() = (false, 0, 100)
  TestInstance/3  # GetParam() = (false, 100, -100)
  TestInstance/4  # GetParam() = (false, 100, 0)
  TestInstance/5  # GetParam() = (false, 100, 100)
  TestInstance/6  # GetParam() = (false, 200, -100)
  TestInstance/7  # GetParam() = (false, 200, 0)
  TestInstance/8  # GetParam() = (false, 200, 100)
  TestInstance/9  # GetParam() = (true, 0, -100)
  TestInstance/10  # GetParam() = (true, 0, 0)
  TestInstance/11  # GetParam() = (true, 0, 100)
  TestInstance/12  # GetParam() = (true, 100, -100)
  TestInstance/13  # GetParam() = (true, 100, 0)
  TestInstance/14  # GetParam() = (true, 100, 100)
  TestInstance/15  # GetParam() = (true, 200, -100)
  TestInstance/16  # GetParam() = (true, 200, 0)
  TestInstance/17  # GetParam() = (true, 200, 100)
DISABLED_ParameterizedTestsTest2/InstantiateDisabledTest.
  TestInstance/0  # GetParam() = -100
  TestInstance/1  # GetParam() = 0
  TestInstance/2  # GetParam() = 100
ParameterizedTestsTest3/NameDisabledTest.
  DISABLED_TestInstance/0  # GetParam() = -100
  DISABLED_TestInstance/1  # GetParam() = 0
  DISABLED_TestInstance/2  # GetParam() = 100
ParameterizedTestsTest4/DISABLED_ClassDisabledTest.
  TestInstance/0  # GetParam() = -100
  TestInstance/1  # GetParam() = 0
  TestInstance/2  # GetParam() = 100
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

[<TestClass>]
type ``GoogleTestDiscoverer`` () =
    let doTestCase (a:string) (b:string) =
        let fn = sprintf "%s.%s" a b
        let tc = TestCase(fn, Uri("http://none"), "ff.exe")
        tc
        
    [<TestMethod>] member x.``matches test executable name`` () =
                    let isGtest = DiscovererUtils.isGoogleTestExecutable (Logger())
                    isGtest "MyGoogleTests.exe" |> should be True
                    isGtest "MyGoogleTest.exe" |> should be True
                    isGtest "mygoogletests.exe" |> should be True
                    isGtest "mygoogletest.exe" |> should be True
                    isGtest "MyGoogleTes.exe" |> should be False
                    isGtest "TotallyWrong.exe" |> should be False
                    isGtest "TestStuff.exe" |> should be True
                    isGtest "TestLibrary.exe" |> should be True
    
    [<TestMethod>] member x.``parses test case list`` () =
                    let result = DiscovererUtils.parseTestCases (gtestBasicMethods.Split([|'\n'|]) |> List.ofArray)
                    result.Length |> should equal 9
                    result.[0] |> should equal ("SecondGoogleTestSuiteName", "SecondSomething")
                    result.[1] |> should equal ("SecondGoogleTestSuiteName", "FirstTestMethodName")
                    result.[2] |> should equal ("GoogleTestSuiteName1", "TestMethod_007")
                    result.[3] |> should equal ("GoogleTestSuiteName1", "TestMethod_006")
                    result.[4] |> should equal ("GoogleTestSuiteName1", "TestMethod_005")

    [<TestMethod>] member x.``parses test case list from googletest 1.7.0 parameterized output`` () =
                    let result = DiscovererUtils.parseTestCases (gtest170ParameterizedMethods.Split([|'\n'|]) |> List.ofArray)
                    result.Length |> should equal 27
                    result.[0] |> should equal ("ParameterizedTestsTest4/DISABLED_ClassDisabledTest", "TestInstance/2")
                    result.[1] |> should equal ("ParameterizedTestsTest4/DISABLED_ClassDisabledTest", "TestInstance/1")
                    result.[2] |> should equal ("ParameterizedTestsTest4/DISABLED_ClassDisabledTest", "TestInstance/0")
                    result.[3] |> should equal ("ParameterizedTestsTest3/NameDisabledTest", "DISABLED_TestInstance/2")
                    result.[4] |> should equal ("ParameterizedTestsTest3/NameDisabledTest", "DISABLED_TestInstance/1")
                    result.[5] |> should equal ("ParameterizedTestsTest3/NameDisabledTest", "DISABLED_TestInstance/0")
                    result.[6] |> should equal ("DISABLED_ParameterizedTestsTest2/InstantiateDisabledTest", "TestInstance/2")
                    result.[7] |> should equal ("DISABLED_ParameterizedTestsTest2/InstantiateDisabledTest", "TestInstance/1")
                    result.[8] |> should equal ("DISABLED_ParameterizedTestsTest2/InstantiateDisabledTest", "TestInstance/0")
                    result.[9] |> should equal ("ParameterizedTestsTest1/AllEnabledTest", "TestInstance/17")
                    result.[10] |> should equal ("ParameterizedTestsTest1/AllEnabledTest", "TestInstance/16")



    [<TestMethod>] member x.``parses test case list from googletest 1.7.0 output`` () =
                    let result = DiscovererUtils.parseTestCases (gtest170TypedMethods.Split([|'\n'|]) |> List.ofArray)
                    result.Length |> should equal 10
                    result.[0] |> should equal ("My/DISABLED_TypedTestP/1", "ShouldNotRun")
                    result.[1] |> should equal ("My/DISABLED_TypedTestP/0", "ShouldNotRun")
                    result.[2] |> should equal ("My/TypedTestP/1", "DISABLED_ShouldNotRun")
                    result.[3] |> should equal ("My/TypedTestP/0", "DISABLED_ShouldNotRun")
                    result.[4] |> should equal ("DISABLED_TypedTest/1", "ShouldNotRun")
                
    [<TestMethod>] member x.``finds tests from statically linked executable with source file locations`` (location) =
                    let tests = DiscovererUtils.getTestsFromExecutable (Logger()) location
                    tests.Length |> should equal 2
                    tests.[0].DisplayName |> should equal "FooTest.DoesXyz"
                    tests.[1].DisplayName |> should equal "FooTest.MethodBarDoesAbc"
                    tests.[0].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp"
                    tests.[1].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp"
                    tests.[0].LineNumber |> should equal 45
                    tests.[1].LineNumber |> should equal 36
                
    [<TestMethod>] member x.``finds tests from statically linked x86 executable with source file locations`` () =
                    x.``finds tests from statically linked executable with source file locations`` x86staticallyLinkedTests

    [<TestMethod>] member x.``finds tests from statically linked x64 executable with source file locations`` () =
                    x.``finds tests from statically linked executable with source file locations`` x64staticallyLinkedTests
                
    [<TestMethod>] member x.``finds tests from externally linked executable with source file locations`` (location) =
                    let tests = DiscovererUtils.getTestsFromExecutable (Logger()) location
                    tests.Length |> should equal 2
                    tests.[0].DisplayName |> should equal "BarTest.DoesXyz"
                    tests.[1].DisplayName |> should equal "BarTest.MethodBarDoesAbc"
                    tests.[0].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp"
                    tests.[1].CodeFilePath |> should equal @"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp"
                    tests.[0].LineNumber |> should equal 44
                    tests.[1].LineNumber |> should equal 36
                
    [<TestMethod>] member x.``finds tests from externally linked x86 executable with source file locations`` () =
                    x.``finds tests from externally linked executable with source file locations`` x86externallyLinkedTests

    [<TestMethod>] member x.``finds tests from externally linked x64 executable with source file locations`` () =
                    x.``finds tests from externally linked executable with source file locations`` x64externallyLinkedTests
