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
open MBT.Operations
open MBT.Messages
open System
open System.IO
open System.Runtime.InteropServices

type private FileArchiveResult =
   | Success of Map<String, String>
   | FailedCouldNotReadFile
   | FailedFileTooBig
   | UnknownError of Exception

///<summary>An actor that archives files</summary>
type Archiver(parent : IActor) =
   inherit ActorBase<ArchiveMessage, UnitPlaceHolder>(parent)

   (* Private Static Fields *)
   static member private DiskFullHResult = 0x70
   static member private ErrorHandleDiskFullHResult = 0x27

   (* Private Methods *)
   member private this.CalculateArchiveFilePath archivePath fileToArchive =
      let fileName = Path.GetFileName(fileToArchive)
      let archivePath = Path.Combine(archivePath, fileName)
      if archivePath = Path.Combine(archivePath, Constants.FileManifestFileName) then
         let fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileToArchive)
         let ext = Path.GetExtension(fileToArchive)
         Path.Combine(archivePath, fileNameWithoutExt, "_archive_file", ext)
      else
         archivePath

   member private this.IsDiskOutOfSpace (ex : IOException) =
      let hr = Marshal.GetHRForException(ex)
      hr = Archiver.DiskFullHResult || hr = Archiver.ErrorHandleDiskFullHResult

   member private this.HandleIOException ex = if this.IsDiskOutOfSpace ex then FailedFileTooBig else FailedCouldNotReadFile

   member private this.TryCopy fileA fileB (fileManifest : Map<String, String>) =
      try
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
      (Success(fileManifest))

   member private this.HandleArchiveResolverResponse (response : ArchiveResolverResponse) =
      let foldFunc (state : Map<String, String> * Map<String, FileArchiveResult>) file =
         let manifest = fst state
         let resultMap = snd state
         let fileArchiveResult = this.ArchiveFile response.ArchiveFilePath file manifest

         match fileArchiveResult with
         | Success(freshManifest) -> (freshManifest, resultMap.Add(file, fileArchiveResult))
         | FailedCouldNotReadFile -> (manifest, resultMap.Add(file, FailedCouldNotReadFile))
         | FailedFileTooBig -> (manifest, resultMap.Add(file, FailedFileTooBig))
         | UnknownError(ex) -> (manifest, resultMap.Add(file, UnknownError(ex)))
      
      Seq.fold foldFunc (response.FileManifest, Map.empty) response.Files
   
   member private this.WriteManifestFile archivePath fileManifest =
      let serializedContext = JsonConvert.SerializeObject(fileManifest)
      let manifestFilePath = Path.Combine(archivePath, Constants.FileManifestFileName)
      File.WriteAllText(manifestFilePath, serializedContext)
   
   member private this.GetFilesTooLarge fileArchiveResultMap =
      ()

   member private this.GetFilesUnableToOpen fileArchiveResultMap =
      ()

   member private this.GetBackedUpFiles fileArchiveResultMap =
      ()

   member private this.FormArchiveResponse fileManifest fileArchiveResultMap =
      ()

   override this.Receive sender msg state =
      Hold

   override this.PreStart() = Hold

   override this.UnknownMessageHandler sender msg initialState =
      match msg with
      | :? ArchiveResolverResponse as response -> 
         let archiveResult = this.HandleArchiveResolverResponse response
         
         ()
      | _ -> ()

      Hold
