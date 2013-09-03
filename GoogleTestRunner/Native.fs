module Native
#nowarn "9" // Uses of this construct may result in the generation of unverifiable .NET IL code.
#nowarn "51" // The use of native pointers may result in unverifiable .NET IL code.

open System
open System.IO
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop
open Dia

[<Struct>]
[<System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>]
type LOADED_IMAGE =
    val mutable ModuleName : IntPtr
    val mutable hFile : IntPtr
    val mutable MappedAddress : IntPtr
    val mutable FileHeader : IntPtr
    val mutable LastRvaSection : IntPtr
    val mutable NumbOfSections : uint32
    val mutable FirstRvaSection : IntPtr
    val mutable charachteristics : uint32
    val mutable systemImage : uint16
    val mutable dosImage : uint16
    val mutable readOnly : uint16
    val mutable version : uint16
    val mutable links_1 : IntPtr
    val mutable links_2 : IntPtr
    val mutable sizeOfImage : uint32
    val mutable links_3 : IntPtr
    val mutable links_4 : IntPtr
    val mutable links_5 : IntPtr
    
[<Struct>]
[<StructLayout(LayoutKind.Explicit)>]
type IMAGE_IMPORT_DESCRIPTOR =
    [<FieldOffset(0)>]
    val mutable Characteristics : uint32
    [<FieldOffset(0)>]
    val mutable OriginalFirstThunk : uint32

    [<FieldOffset(4)>]
    val mutable TimeDateStamp : uint32
    [<FieldOffset(8)>]
    val mutable ForwarderChain : uint32
    [<FieldOffset(12)>]
    val mutable Name : uint32
    [<FieldOffset(16)>]
    val mutable FirstThunk : uint32

[<DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi)>]
extern bool MapAndLoad(string imageName, string dllPath, LOADED_IMAGE* loadedImage, bool dotDll, bool readOnly)

[<DllImport("imageHlp.dll", CallingConvention = CallingConvention.Winapi)>]
extern bool UnMapAndLoad(LOADED_IMAGE& loadedImage);

[<DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)>]
extern IMAGE_IMPORT_DESCRIPTOR* ImageDirectoryEntryToData(IntPtr pBase, bool mappedAsImage, uint16 directoryEntry, uint32* size)

[<DllImport("dbghelp.dll", CallingConvention = CallingConvention.Winapi)>]
extern IntPtr ImageRvaToVa(
    IntPtr pNtHeaders,
    IntPtr pBase,
    uint32 rva,
    IntPtr pLastRvaSection);

/// Releases a COM object
let releaseCom obj = System.Runtime.InteropServices.Marshal.FinalReleaseComObject obj |> ignore

/// Currently only loads the names of imported modules
type PeParser(fileName) =
    let mutable loadedImage : LOADED_IMAGE = new LOADED_IMAGE()
    let mutable imports : (string) list = List.empty
    let getString rva =
        ImageRvaToVa(loadedImage.FileHeader, loadedImage.MappedAddress, rva, IntPtr.Zero)
            |> Marshal.PtrToStringAnsi
    do
        if MapAndLoad(fileName, null, &&loadedImage, true, true) then
            let mutable size = 0u
            let mutable directoryEntryPtr = ImageDirectoryEntryToData(loadedImage.MappedAddress, false, 1us, &&size)
            let mutable directoryEntry = NativePtr.get directoryEntryPtr 0
            while directoryEntry.OriginalFirstThunk <> 0u do
                imports <- (getString directoryEntry.Name) :: imports
                directoryEntryPtr <- NativePtr.add directoryEntryPtr 1
                directoryEntry <- NativePtr.get directoryEntryPtr 0
            UnMapAndLoad(&loadedImage) |> ignore
        ()

    /// Get the list of imported modules
    member x.Imports() = imports
