module PeTest
open System
open System.Diagnostics
open FsUnit
open NUnit.Framework

[<TestFixture>] 
type ``Test PeParser`` ()=
    let path = @"kernel32.dll"

    [<Test>]
    member x.``read imports`` ()=
        let pe = Native.PeParser(path)
        let imports = pe.Imports()
        imports.Length |> should equal 24
