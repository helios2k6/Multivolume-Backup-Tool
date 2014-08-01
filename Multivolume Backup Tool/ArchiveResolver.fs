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
open MBT.Core.Utilities
open MBT.Core.Seq
open MBT.Core.Monads
open System
open System.IO
open System.Collections.Generic

type private FileDecision = KeepArchiveFile | AddOrReplaceArchiveFile

/// <summary>
/// The archive resolving actor
/// </summary>
type ArchiveResolver(parent : IActor) as this =
   inherit ActorBase<ArchiveResolverMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)
   let TryDeserializeManifestFile fileContents =
      try
         PrintToConsole "Deserializing file manifest"
         JsonConvert.DeserializeObject<Dictionary<String, String>>(fileContents)
         |> Seq.map (|KeyValue|)
         |> Map.ofSeq
         |> Some
      with
         | ex -> 
            PrintToConsole <| sprintf "Could not deserialize file manifest. Reason: %A" ex
            None
   let TryReadManifestFile archiveFilePath =
      let pathOfManifestFile = Path.Combine(archiveFilePath, Constants.FileManifestFileName)
      if File.Exists(pathOfManifestFile) then
         File.ReadAllText(pathOfManifestFile) |> TryDeserializeManifestFile
      else
         None

   let TranslateRawManifest (fileManifest : Map<String, String>) =
      let mapAction tuple = 
         async {
            let sourceFilePath = fst tuple
            let destFilePath = snd tuple
         
            if File.Exists sourceFilePath && File.Exists destFilePath then
               PrintToConsole <| sprintf "Found existing file mapping %A => %A" sourceFilePath destFilePath
               return Some (new FileEntry(sourceFilePath), new FileEntry(destFilePath))
            else
               PrintToConsole <| sprintf "Did not find the file mapping %A => %A" sourceFilePath destFilePath
               return None
         }

      fileManifest
      |> Seq.map (|KeyValue|)
      |> Seq.map mapAction
      |> Async.Parallel
      |> Async.RunSynchronously
      |> Seq.UnwrapOptionalSeq
      |> Map.ofSeq

   let ProcessFileFromExistingArchive (oldFileManifest : FileManifest) (fileToBackUp : FileEntry) =
      let item = oldFileManifest.TryFind fileToBackUp
      match item with
      | Some(oldArchivedFile) -> 
         if fileToBackUp.Info.LastWriteTimeUtc > oldArchivedFile.Info.LastWriteTimeUtc then
            PrintToConsole <| sprintf "Adding or replacing %A" oldArchivedFile.Path
            AddOrReplaceArchiveFile
         else
            PrintToConsole <| sprintf "Retaining archive file %A" oldArchivedFile.Path
            KeepArchiveFile
      | None -> 
         PrintToConsole <| sprintf "Could not find file %A in the existing archive. Adding" fileToBackUp.Path
         AddOrReplaceArchiveFile
   
   let ProcessExistingArchive oldFileManifest filesToBackup =
      PrintToConsole "Processing existing archive"
      let processedFileTuples = List.map (fun file -> (file, ProcessFileFromExistingArchive oldFileManifest file)) filesToBackup
      let filesToAddOrReplace = processedFileTuples |> List.filter (fun item -> snd item = AddOrReplaceArchiveFile) |> List.map (fun item -> fst item) |> Set.ofList
      let filesToKeep = oldFileManifest |> Seq.map (fun item -> item.Key) |> Seq.filter (fun item -> not <| filesToAddOrReplace.Contains item) |> Seq.toList
      let newFileManifest = filesToKeep |> Seq.map (fun item -> (item, oldFileManifest.Item item)) |> Map.ofSeq

      (newFileManifest, filesToAddOrReplace |> Set.toList)

   let SendEmptyResponse (sender : IActor) archiveFilePath files =
      sender +! Message.Compose this { ArchiveFilePath = archiveFilePath; FileManifest = Map.empty; Files = files; }
   
   let SendUpdatedManifest (sender : IActor) archiveFilePath manifest files =
      sender +! Message.Compose this { ArchiveFilePath = archiveFilePath; FileManifest = manifest; Files = files; }

   (* Public Methods *)
   override this.Receive sender msg _ =
      let translatedManifestFile = maybe {
         let! manifest = TryReadManifestFile msg.ArchiveFilePath
         return TranslateRawManifest manifest 
      }

      match translatedManifestFile with
      | Some(manifestFile) -> 
         let newFileManifestAndProcessedFiles = ProcessExistingArchive manifestFile msg.Files
         SendUpdatedManifest sender msg.ArchiveFilePath (fst newFileManifestAndProcessedFiles) (snd newFileManifestAndProcessedFiles)
      | None -> SendEmptyResponse sender msg.ArchiveFilePath msg.Files

      Some Hold

   override this.PreStart() = Hold