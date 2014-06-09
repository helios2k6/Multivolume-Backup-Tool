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
open System
open System.IO
open System.Collections.Generic

type private FileDecision =
   | KeepArchiveFile
   | AddOrReplaceArchiveFile

type ArchiveResolver(parent : IActor) =
   inherit ActorBase<ArchiveResolverMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)
   member private this.TryDeserializeManifestFile fileContents =
      try
         PrintToConsole "Desterializing file manifest"
         JsonConvert.DeserializeObject<Dictionary<String, String>>(fileContents)
         |> Seq.map (|KeyValue|)
         |> Map.ofSeq
         |> Some
      with
         | ex -> 
            PrintToConsole <| sprintf "Could not deserialize file manifest. Reason: %A" ex
            None

   member private this.TryReadManifestFile archiveFilePath =
      let pathOfManifestFile = Path.Combine(archiveFilePath, Constants.FileManifestFileName)
      if File.Exists(pathOfManifestFile) then
         PrintToConsole <| sprintf "Reading file manifest at"
         File.ReadAllText(pathOfManifestFile)
         |> this.TryDeserializeManifestFile
      else
         PrintToConsole "File manifest does not exist"
         None

   member private this.IsDiskFileNewerThanArchiveFile diskFile oldArchiveFile =
      let oldFileInfo = new FileInfo(oldArchiveFile)
      let newFileInfo = new FileInfo(diskFile)

      newFileInfo.LastWriteTimeUtc > oldFileInfo.LastWriteTimeUtc

   member private this.ProcessFileFromExistingArchive (oldFileManifest : Map<String, String>) fileToBackUp =
      let item = oldFileManifest.TryFind fileToBackUp
      match item with
      | Some(oldArchivedFile) -> 
         if not <| File.Exists oldArchivedFile || this.IsDiskFileNewerThanArchiveFile fileToBackUp oldArchivedFile then
            AddOrReplaceArchiveFile
         else
            KeepArchiveFile
      | None -> AddOrReplaceArchiveFile
   
   member private this.ProcessExistingArchive oldFileManifest filesToBackup =
      PrintToConsole "Processing existing archive"
      let processedFileTuples = Seq.map (fun file -> (file, this.ProcessFileFromExistingArchive oldFileManifest file)) filesToBackup
      let filesToAddOrReplace = processedFileTuples |> Seq.filter (fun item -> snd item = AddOrReplaceArchiveFile) |> Seq.map (fun item -> fst item) |> Set.ofSeq
      let filesToKeep = oldFileManifest |> Seq.map (fun item -> item.Key) |> Seq.filter (fun item -> not <| filesToAddOrReplace.Contains item)
      let newFileManifest = filesToKeep |> Seq.map (fun item -> (item, oldFileManifest.Item item)) |> Map.ofSeq

      (newFileManifest, filesToAddOrReplace :> String seq)

   member private this.SendEmptyResponse (sender : IActor) archiveFilePath files client =
      sender +! Message.Compose this { ArchiveFilePath = archiveFilePath; FileManifest = Map.empty; Files = files; Client = client }
   
   member private this.SendUpdatedManifest (sender : IActor) archiveFilePath manifest files client =
      sender +! Message.Compose this { ArchiveFilePath = archiveFilePath; FileManifest = manifest; Files = files; Client = client }

   (* Public Methods *)
   override this.Receive sender msg state =
      let existingManifestFile = this.TryReadManifestFile msg.ArchiveFilePath
      match existingManifestFile with
      | Some(manifestFile) -> 
         let newFileManifestAndProcessedFiles = this.ProcessExistingArchive manifestFile msg.Files
         this.SendUpdatedManifest sender msg.ArchiveFilePath (fst newFileManifestAndProcessedFiles) (snd newFileManifestAndProcessedFiles) msg.Client
      | None -> this.SendEmptyResponse sender msg.ArchiveFilePath msg.Files msg.Client

      Hold

   override this.PreStart() = Hold