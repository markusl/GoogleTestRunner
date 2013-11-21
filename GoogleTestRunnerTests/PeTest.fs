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
        let v = Environment.OSVersion.Version
        // W7
        if v.Major = 6 && v.Minor = 1 then
            imports.Length |> should equal 24
        // W8
        if v.Major = 6 && v.Minor = 2 then
            imports.Length |> should equal 49
