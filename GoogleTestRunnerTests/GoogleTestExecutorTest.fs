namespace GoogleTestRunnerTests
open GoogleTestRunner
open GoogleTestDiscovererTest
open System
open FsUnit
open Foq
open NUnit.Framework
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter

[<TestFixture>] 
type ``Test GoogleTestExecutor`` () =
    let mutable results = List.empty
    let executor = GoogleTestExecutor() :> ITestExecutor
    let ctx = Mock<IRunContext>().Create()
        
    let runAndVerifyTests (location:string) passed failed =
        let testIsPassed (tc:TestResult) = tc.Outcome = TestOutcome.Passed
        let testIsFailed (tc:TestResult) = tc.Outcome = TestOutcome.Failed

        let handle = Mock<IFrameworkHandle>().Create()
        executor.RunTests([location], ctx, handle)
        Mock.Verify(<@ handle.RecordResult(is(testIsPassed)) @>, exactly passed)
        Mock.Verify(<@ handle.RecordResult(is(testIsFailed)) @>, exactly failed)
                
    [<Test>] member x.``runs all tests from x86 externally linked tests`` () =
                runAndVerifyTests x86externallyLinkedTests 2 0
                
    [<Test>] member x.``runs all tests from x64 externally linked tests`` () =
                runAndVerifyTests x64externallyLinkedTests 2 0
                
    [<Test>] member x.``runs all tests from x86 statically linked tests`` () =
                runAndVerifyTests x86staticallyLinkedTests 1 1
                
    [<Test>] member x.``runs all tests from x64 statically linked tests`` () =
                runAndVerifyTests x64staticallyLinkedTests 1 1
                
    [<Test>] member x.``runs crashing x64 tests without results`` () =
                runAndVerifyTests x64crashingTests 0 0
                
    [<Test>] member x.``runs crashing x86 tests without results`` () =
                runAndVerifyTests x86crashingTests 0 0
                
    [<Test>] member x.``run single crashing x64 test`` () =
                let handle = Mock<IFrameworkHandle>().Create()
                let tc = TestCase("CrashTestSuite.CrashThisApplication", Uri("executor://GoogleTestRunner/v1"), x64crashingTests)
                executor.RunTests([tc], ctx, handle)
                let testIsFailed (tc:TestResult) =
                    tc.ErrorMessage |> should equal "unknown file\nSEH exception with code 0xc0000005 thrown in the test body."
                    tc.Outcome = TestOutcome.Failed
                Mock.Verify(<@ handle.RecordResult(is(testIsFailed)) @>, exactly 1)
                