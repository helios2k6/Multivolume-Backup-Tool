(*
 * The MIT License (MIT)
 *
 * Copyright (c) 2014 Andrew B. Johnson
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *)

namespace MBT

open MBT.Core
open MBT.Core.Utilities
open MBT.Core.Tuple
open MBT.Operations
open MBT.Messages
open System
open System.Collections.Generic
open System.IO
open System.Runtime.InteropServices

type private FileArchiveResult =
   | Success
   | FailedCouldNotReadFile
   | FailedFileTooBig
   | UnknownError of Exception

///<summary>An actor that archives files</summary>
type Archiver(parent : IActor) =
   inherit ActorBase<ArchiveMessage, UnitPlaceHolder>(parent)

   (* Private Static Fields *)
   static let (|KeyOnly|) (kvp : KeyValuePair<_, _>) = kvp.Key
   static let DiskFullHResult = 0x70
   static let ErrorHandleDiskFullHResult = 0x27

   (* Private Methods *)
   let PrintResult (result : FileEntry * String * FileArchiveResult) =
      match result with
      (originalFile, backedUpFile, fileArchiveResult) -> 
         match fileArchiveResult with
         | Success -> PrintToConsole <| sprintf "Successfully archived file %s to %s" originalFile.Path backedUpFile
         | FailedCouldNotReadFile -> PrintToConsole <| sprintf "Could not archive file %s. Failed to read file" originalFile.Path
         | FailedFileTooBig -> PrintToConsole <| sprintf "Could not archive file %s. File too big" originalFile.Path
         | UnknownError(ex) -> PrintToConsole <| sprintf "Could not archive file %s. Unknown error: %A" originalFile.Path ex

   let RerootPath newRoot (fileEntry : FileEntry) =
      let filePath = fileEntry.Path
      let pathRoot = Path.GetPathRoot(filePath)
      let derootedPath = filePath.Replace(pathRoot, String.Empty)
      Path.Combine(newRoot, derootedPath)

   let CalculateArchiveFilePath archivePath (fileToArchive : FileEntry) =
      let calculatedArchiveFilePath = RerootPath archivePath fileToArchive
      if calculatedArchiveFilePath = Path.Combine(archivePath, Constants.FileManifestFileName) then
         let fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileToArchive.Path)
         let ext = Path.GetExtension(fileToArchive.Path)
         Path.Combine(archivePath, fileNameWithoutExt, "_archive_file", ext)
      else
         calculatedArchiveFilePath

   let IsDiskOutOfSpace (ex : IOException) =
      let hr = Marshal.GetHRForException(ex)
      hr = DiskFullHResult || hr = ErrorHandleDiskFullHResult

   let HandleIOException ex = if IsDiskOutOfSpace ex then FailedFileTooBig else FailedCouldNotReadFile
   
   let CreateIntermediateDirectoryStructure filePath =
      if not <| File.Exists(filePath) then
         let fileName = Path.GetFileName(filePath)
         let directoryOnly = filePath.Replace(fileName, String.Empty)
         if not <| Directory.Exists(directoryOnly) then
            PrintToConsole <| sprintf "Creating directory tree: %s" directoryOnly
            Directory.CreateDirectory(directoryOnly) |> ignore

   let TryCopy fileA fileB =
      try
      (*
         CreateIntermediateDirectoryStructure fileB
         File.Copy(fileA, fileB, true)
         *)
         Success
      with
         | :? UnauthorizedAccessException as ex -> UnknownError(ex)
         | :? ArgumentException as ex -> UnknownError(ex)
         | :? PathTooLongException as ex -> UnknownError(ex)
         | :? DirectoryNotFoundException as ex -> UnknownError(ex)
         | :? FileNotFoundException as ex -> FailedCouldNotReadFile
         | :? IOException as ex -> HandleIOException ex
         | :? NotSupportedException as ex -> UnknownError(ex)

   let ArchiveFile archiveFilePath (filePath : FileEntry) = 
      let filePathToCopyTo = CalculateArchiveFilePath archiveFilePath filePath
      let result = TryCopy filePath.Path filePathToCopyTo

      (filePath, filePathToCopyTo, result)

   let BackupFiles archiveFilePath (files : FileEntry list) =
      let foldFunc (state : (FileEntry * String * FileArchiveResult) list) file =
         let result = ArchiveFile archiveFilePath file
         PrintResult result
         result :: state

      PrintToConsole "Beginning archive process"
      List.fold foldFunc list.Empty files
   
   let FilterForResult (archiveResultSequence : (FileEntry * String * FileArchiveResult) list) result =
      let matchResult item = (thrdOfThree item) = result

      archiveResultSequence
      |> List.filter matchResult
      |> List.map fstOfThree

   let GetFilesTooLarge (archiveResultSequence : (FileEntry * String * FileArchiveResult) list) = FilterForResult archiveResultSequence FailedFileTooBig

   let GetFilesUnableToOpen (archiveResultSequence : (FileEntry * String * FileArchiveResult) list) = FilterForResult archiveResultSequence FailedCouldNotReadFile

   let GetBackedUpFiles (archiveResultSequence : (FileEntry * String * FileArchiveResult) list) =
      let matchUp tuple = (thrdOfThree tuple) = Success

      archiveResultSequence
      |> List.filter matchUp
      |> List.map (fun item -> (fstOfThree item, new FileEntry(sndOfThree item)))

   let PrintBackedUpFileStatistics (backedUpFiles : FileEntry list) =
      let foldAction state (item : FileEntry) = state + item.Info.Length |> (|MebiBytes|)

      backedUpFiles
      |> List.fold foldAction 0L
      |> sprintf "Total Amount Archived: %A Mebibytes"
      |> PrintToConsole

   let FormArchiveResponse (archiveResultSequence : (FileEntry * String * FileArchiveResult) list) =
      let backedUpFiles = GetBackedUpFiles archiveResultSequence
      let unableToOpen = GetFilesUnableToOpen archiveResultSequence
      let filesTooBig = GetFilesTooLarge archiveResultSequence

      backedUpFiles |> List.map fst |> PrintBackedUpFileStatistics

      { BackedUpFiles = backedUpFiles; UnableToOpenFiles = unableToOpen; FilesTooBig = filesTooBig }

   override this.Receive sender msg _ =
      let response = BackupFiles msg.ArchiveFilePath msg.Files |> FormArchiveResponse
      sender +! Message.Compose this response
      Some Hold

   override this.PreStart() = Hold