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

open Newtonsoft.Json
open MBT.Core
open MBT.Core.Utilities
open MBT.Operations
open MBT.Messages
open System
open System.Collections.Generic
open System.IO
open System.Runtime.InteropServices

type private FileArchiveResult =
   | Success of Map<String, String>
   | FailedCouldNotReadFile
   | FailedFileTooBig
   | UnknownError of Exception

///<summary>An actor that archives files</summary>
type Archiver(parent : IActor) as this =
   inherit ActorBase<ArchiveMessage, UnitPlaceHolder>(parent)

   (* Private Static Fields *)
   static let (|KeyOnly|) (kvp : KeyValuePair<_, _>) = kvp.Key
   static let DiskFullHResult = 0x70
   static let ErrorHandleDiskFullHResult = 0x27

   (* Private Fields *)
   let _archiveResolver = new ArchiveResolver(this)

   (* Private Methods *)
   member private this.RerootPath filePath newRoot =
      let pathRoot = Path.GetPathRoot(filePath)
      let derootedPath = filePath.Replace(pathRoot, String.Empty)
      Path.Combine(newRoot, derootedPath)

   member private this.CalculateArchiveFilePath archivePath fileToArchive =
      let calculatedArchiveFilePath = this.RerootPath fileToArchive archivePath
      if calculatedArchiveFilePath = Path.Combine(archivePath, Constants.FileManifestFileName) then
         let fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileToArchive)
         let ext = Path.GetExtension(fileToArchive)
         Path.Combine(archivePath, fileNameWithoutExt, "_archive_file", ext)
      else
         calculatedArchiveFilePath

   member private this.IsDiskOutOfSpace (ex : IOException) =
      let hr = Marshal.GetHRForException(ex)
      hr = DiskFullHResult || hr = ErrorHandleDiskFullHResult

   member private this.HandleIOException ex = if this.IsDiskOutOfSpace ex then FailedFileTooBig else FailedCouldNotReadFile
   
   member private this.CreateIntermediateDirectoryStructure filePath =
      if not <| File.Exists(filePath) then
         let fileName = Path.GetFileName(filePath)
         let directoryOnly = filePath.Replace(fileName, String.Empty)
         if not <| Directory.Exists(directoryOnly) then
            PrintToConsole <| sprintf "Creating directory tree: %s" directoryOnly
            Directory.CreateDirectory(directoryOnly) |> ignore

   member private this.TryCopy fileA fileB (fileManifest : Map<String, String>) =
      try
         this.CreateIntermediateDirectoryStructure fileB
         PrintToConsole <| sprintf "Copying file %s -> %s" fileA fileB
         File.Copy(fileA, fileB, true)

         Success(fileManifest.Add(fileA, fileB))
      with
         | :? UnauthorizedAccessException as ex -> UnknownError(ex)
         | :? ArgumentException as ex -> UnknownError(ex)
         | :? PathTooLongException as ex -> UnknownError(ex)
         | :? DirectoryNotFoundException as ex -> UnknownError(ex)
         | :? FileNotFoundException as ex -> FailedCouldNotReadFile
         | :? IOException as ex -> this.HandleIOException ex
         | :? NotSupportedException as ex -> UnknownError(ex)

   member private this.ArchiveFile archiveFilePath filePath (fileManifest : Map<String, String>) = 
      let calculatedArchiveFilePath = this.CalculateArchiveFilePath archiveFilePath filePath
      this.TryCopy filePath calculatedArchiveFilePath fileManifest

   member private this.HandleArchiveResolverResponse (response : ArchiveResolverResponse) =
      let foldFunc (state : Map<String, String> * Map<String, FileArchiveResult>) file =
         PrintToConsole <| sprintf "Archiving file %s" file
         let manifest = fst state
         let resultMap = snd state
         let fileArchiveResult = this.ArchiveFile response.ArchiveFilePath file manifest

         match fileArchiveResult with
         | Success(freshManifest) -> 
            PrintToConsole <| sprintf "Successfully archived file %s" file
            (freshManifest, resultMap.Add(file, fileArchiveResult))
         | FailedCouldNotReadFile -> 
            PrintToConsole <| sprintf "Could not archive file %s. Failed to read file" file
            (manifest, resultMap.Add(file, FailedCouldNotReadFile))
         | FailedFileTooBig -> 
            PrintToConsole <| sprintf "Could not archive file %s. File too big" file
            (manifest, resultMap.Add(file, FailedFileTooBig))
         | UnknownError(ex) -> 
            PrintToConsole <| sprintf "Could not archive file %s. Unknown error: %A" file ex
            (manifest, resultMap.Add(file, UnknownError(ex)))

      PrintToConsole "Beginning archive process"
      Seq.fold foldFunc (response.FileManifest, Map.empty) response.Files
   
   member private this.WriteManifestFile archivePath fileManifest =
      let serializedContext = JsonConvert.SerializeObject(fileManifest, Formatting.Indented)
      let manifestFilePath = Path.Combine(archivePath, Constants.FileManifestFileName)
      PrintToConsole <| sprintf "Writing manifest file to: %s" manifestFilePath
      File.WriteAllText(manifestFilePath, serializedContext)
   
   member private this.FilterForResult (fileArchiveResultMap : Map<String, FileArchiveResult>) result =
      fileArchiveResultMap
      |> Seq.filter (fun tuple -> tuple.Value = result)
      |> Seq.map (|KeyOnly|)

   member private this.GetFilesTooLarge (fileArchiveResultMap : Map<String, FileArchiveResult>) =
      this.FilterForResult fileArchiveResultMap FailedFileTooBig

   member private this.GetFilesUnableToOpen (fileArchiveResultMap : Map<String, FileArchiveResult>) =
      this.FilterForResult fileArchiveResultMap FailedCouldNotReadFile

   member private this.GetBackedUpFiles (fileArchiveResultMap : Map<String, FileArchiveResult>) =
      let matchUp (tuple : KeyValuePair<_, _>) = match tuple.Value with
                                                 | Success(_) -> true
                                                 | _ -> false

      fileArchiveResultMap
      |> Seq.filter matchUp
      |> Seq.map (|KeyOnly|)

   member private this.FormArchiveResponse fileManifest fileArchiveResultMap =
      let backedUpFiles = this.GetBackedUpFiles fileArchiveResultMap
      let unableToOpen = this.GetFilesUnableToOpen fileArchiveResultMap
      let filesTooBig = this.GetFilesTooLarge fileArchiveResultMap
      PrintToConsole "Messaging client with results"
      { BackedUpFiles = backedUpFiles; UnableToOpenFiles = unableToOpen; FilesTooBig = filesTooBig }

   override this.Receive sender msg state =
      _archiveResolver +! Message.Compose this { ArchiveResolverMessage.ArchiveFilePath = msg.ArchiveFilePath; ArchiveResolverMessage.Files = msg.Files; ArchiveResolverMessage.Client = sender }
      Hold

   override this.PreStart() = Hold

   override this.UnknownMessageHandler sender msg initialState =
      match msg with
      | :? ArchiveResolverResponse as response -> 
         PrintToConsole "Received ArchiveResolver response"
         let archiveResult = this.HandleArchiveResolverResponse response
         let fileManifest = fst archiveResult
         let archiveResultMap = snd archiveResult
         let archiveResponse = this.FormArchiveResponse fileManifest archiveResultMap
         PrintToConsole "Finished archiving"
         PrintToConsole <| sprintf "Successfully archived %A file(s)"  (Map.fold (fun state _ _ -> state + 1) 0 fileManifest)
         this.WriteManifestFile response.ArchiveFilePath fileManifest
         response.Client +! Message.Compose this archiveResponse
      | _ -> ()

      Hold