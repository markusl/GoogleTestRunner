module DiaResolver
open Dia
open System
open System.IO
open System.Diagnostics
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

module Array =
    let difference xs ys = 
        let ySet = set ys
        let notInYSet x = not <| Set.contains x ySet
        Array.filter notInYSet xs

type Path =
    /// Replaces the file name extension from a full path.
    static member ReplaceExtension(path, newExtension) =        
        Path.Combine(Path.GetDirectoryName(path),
                     Path.GetFileNameWithoutExtension(path))
            + newExtension
      

let private findSymbolsFromExecutable symbols symbolFilterString (logger : IMessageLogger) executable =
    let matchSymbol s (f:string, _, _, _) = f.EndsWith(s, StringComparison.Ordinal)

    let executableSymbols (session:IDiaSession) executable =
        let diaSymbols = session.findFunctionsByRegex symbolFilterString
        try
            session.getSymbolNamesAndAddresses diaSymbols
        finally
            Native.releaseCom diaSymbols

    let getSourceFileLocation (session:IDiaSession) (symbol, addressSection, addressOffset, length) =
        let lineNumbers = session.getLineNumbers(addressSection, addressOffset, length)
        if (lineNumbers |> Seq.length) > 0 then
            let file, line = lineNumbers |> Seq.nth 0
            symbol, (file, line)
        else 
            logger.SendMessage(TestMessageLevel.Error, "Failed to locate line number for " + symbol)
            symbol, (executable, 112)
        
    logger.SendMessage(TestMessageLevel.Warning, sprintf "Loading symbols from %s" executable)

    let sw = System.Diagnostics.Stopwatch.StartNew()
    let diaDataSource = DiaSourceClass()
    let path = Path.ReplaceExtension(executable, ".pdb")

    DiaMemoryStream(path) |> diaDataSource.loadDataFromIStream
    let diaSession = diaDataSource.openSession()
    try
        let allSymbols = executableSymbols diaSession executable
        let tryFindFromAllSymbols currentSymbol = allSymbols |> List.tryFind (matchSymbol currentSymbol)
        let foundSymbols = symbols |> Array.Parallel.choose tryFindFromAllSymbols
        let symbolInfo = foundSymbols |> Array.map(getSourceFileLocation diaSession)

        sw.Stop()                                
        logger.SendMessage(TestMessageLevel.Warning, sprintf "From %s, found %d symbols in %d ms" executable foundSymbols.Length sw.ElapsedMilliseconds)
        symbolInfo
    finally
        Native.releaseCom diaSession
        Native.releaseCom diaDataSource

/// Maps given symbols in executable to source file names and lines
///
/// executable            The main executable to open to find symbols.
/// symbols               The symbols to look for
/// symbolFilterString    Pre-defined filter that is used to filter only relevant functions from DIA SDK.
///                       Can speed up things dramatically if module contains many symbols.
/// logger                Debug/info logger.
let resolveAllMethods executable symbols symbolFilterString (logger:IMessageLogger)  =
    try
        let foundSymbols = findSymbolsFromExecutable symbols symbolFilterString logger executable
        if foundSymbols.Length <> 0 then foundSymbols
        else
            let parser = Native.PeParser(executable) in
            let moduleDirectory = Path.GetDirectoryName executable in
            logger.SendMessage(TestMessageLevel.Warning, sprintf "Couldn't find %d symbols in %s, looking from DllImports in module directory %s" symbols.Length executable moduleDirectory)
            let foundSymbols = parser.Imports()
                                    |> Array.ofList
                                    |> Array.map (fun f -> Path.Combine(moduleDirectory, f))
                                    |> Array.filter File.Exists
                                    |> Array.map (findSymbolsFromExecutable symbols symbolFilterString logger)
            Array.concat foundSymbols
    with
        | :? System.AggregateException as ae -> 
            for e in ae.InnerExceptions do
                logger.SendMessage(TestMessageLevel.Error, "While loading symbols from " + executable + ": " + e.Message)
                logger.SendMessage(TestMessageLevel.Error, e.StackTrace)
            Array.empty
        | e ->
            logger.SendMessage(TestMessageLevel.Error, e.Message)
            Array.empty
