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
      
let private findSymbolsFromExecutable (symbols:string[]) (logger:IMessageLogger) executable =
    let matchSymbol s (f:string, _, _, _) = f.EndsWith(s, StringComparison.Ordinal)

    let executableSymbols (session:IDiaSession) executable =
        let diaSymbols = session.findFunctions()
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

    let diaDataSource = DiaSourceClass()
    let path = sprintf "%s.pdb" (Path.Combine(Path.GetDirectoryName(executable),
                                                          Path.GetFileNameWithoutExtension(executable)))

    DiaMemoryStream(path) |> diaDataSource.loadDataFromIStream
    let diaSession = diaDataSource.openSession()
    try
        let allSymbols = executableSymbols diaSession executable
        let foundSymbols = symbols
                            |> Array.choose(fun currentSymbol -> allSymbols |> List.tryFind (matchSymbol currentSymbol))
        let symbols = foundSymbols |> Array.map(getSourceFileLocation diaSession)
                                
        logger.SendMessage(TestMessageLevel.Warning, sprintf "From %s, found %d symbols" executable foundSymbols.Length)
        symbols
    finally
        Native.releaseCom diaSession
        Native.releaseCom diaDataSource

/// Maps given symbols in executable to source file names and lines
let resolveAllMethods executable (symbols:string[]) (logger:IMessageLogger)  =
    try
        let foundSymbols = findSymbolsFromExecutable symbols logger executable
        if foundSymbols.Length <> 0 then foundSymbols
        else
            let parser = Native.PeParser(executable) in
            let moduleDirectory = Path.GetDirectoryName executable in
            logger.SendMessage(TestMessageLevel.Warning, sprintf "Couldn't find %d symbols in %s, looking from DllImports in module directory %s" symbols.Length executable moduleDirectory)
            let foundSymbols = parser.Imports()
                                    |> Array.ofList
                                    |> Array.map (fun f -> Path.Combine(moduleDirectory, f))
                                    |> Array.filter File.Exists
                                    |> Array.map (findSymbolsFromExecutable symbols logger)
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
