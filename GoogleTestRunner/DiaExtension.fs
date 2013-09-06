[<AutoOpen>]
module DiaExtension
open Dia
open System
open System.Runtime.InteropServices

type NameSearchOptions =
    | nsNone               = 0x0u
    | nsfCaseSensitive     = 0x1u
    | nsfCaseInsensitive   = 0x2u
    | nsfFNameExt          = 0x4u
    | nsfRegularExpression = 0x8u
    | nsfUndecoratedName   = 0x10u

/// IDiaSession extension
type IDiaSession with
    /// Find all symbols from session's global scope which are tagged as functions
    member x.findFunctions() =
         x.findChildren(x.globalScope, SymTagEnum.SymTagFunction, null, uint32 NameSearchOptions.nsNone)

    /// Find all symbols matching from session's global scope which are tagged as functions
    member x.findFunctionsByRegex(str) =
        let nsRegularExpression = 9u
        x.globalScope.findChildren(SymTagEnum.SymTagFunction, str, uint32 NameSearchOptions.nsfRegularExpression)

    /// From given symbol enumeration, extract name, section, offset and length
    member x.getSymbolNamesAndAddresses(diaSymbols:IDiaEnumSymbols) =
        seq { for s in diaSymbols do
                yield try (let symbol = (s :?> IDiaSymbol)
                           symbol.name,
                           symbol.addressSection,
                           symbol.addressOffset,
                           symbol.length |> uint32)
                      finally Native.releaseCom s } |> List.ofSeq

    member x.getLineNumbers(addressSection, addressOffset, length) =
        let diaLineNumbers = x.findLinesByAddr(addressSection, addressOffset, length)
        try
            if diaLineNumbers.count > 0 then
                seq { for lineNumber in diaLineNumbers do
                        yield try (let ln = (lineNumber :?> IDiaLineNumber)
                                   ln.sourceFile.fileName, int ln.lineNumber)
                              finally Native.releaseCom lineNumber
                    } |> List.ofSeq
            else
                List.empty
        finally Native.releaseCom diaLineNumbers

