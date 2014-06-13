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
open MBT.Messages
open MBT.Operations
open Microsoft.FSharp.Collections
open System

///<summary>The backup manager's state object</summary>
type BackupManagerState = { AllFiles : seq<String>; ProcessedFiles : seq<String>; Configuration : ApplicationConfiguration }

///<summary>The main actor in charge of backing up the system</summary>
type BackupManager(parent : IActor, initialConfig : ApplicationConfiguration) as this =
   inherit ActorBase<BackupMessage, BackupManagerState>(parent)

   (* Private Fields *)
   let _fileChooser = new FileChooser(this)
   let _knapsackSolver = new KnapsackSolver(this)
   let _archiver = new Archiver(this)
   let _archiveResolver = new ArchiveResolver(this)
   let _fileManifestWriter = new FileManifestWriter(this)
   let _continuationManager = new BackupContinuationManager(this)
   let _volumeSwitcher = new VolumeSwitcher(this)
   
   (* Private Methods *)
   let HandleFileChooserResponse response initialState =
      PrintToConsole "Received File Chooser response"
      match response with
      | FileChooserResponse.Files(files) -> 
         _archiveResolver +! Message.Compose this { ArchiveResolverMessage.ArchiveFilePath = initialState.Configuration.ArchiveFilePath; Files = files; }
      | FileChooserResponse.Failure -> parent +! Message.Compose this BackupResponse.Failure

      initialState

   let HandleArchiveResolverResponse (response : ArchiveResolverResponse) initialState =
      PrintToConsole "Received Archive Resolver response"
      let processedFiles = response.FileManifest |> Seq.map (fun tuple -> tuple.Key) |> Set.ofSeq
      let allFilesAsSet = Set.ofSeq initialState.AllFiles
      let filesToProcess = allFilesAsSet - processedFiles
      
      _knapsackSolver +! Message.Compose this (KnapsackMessage.Calculate(initialState.Configuration.ArchiveFilePath, filesToProcess))
      { initialState with ProcessedFiles = processedFiles }

   let HandleKnapsackMessage response initialState =
      match response with
      | KnapsackResponse.Files(files) -> 
         PrintToConsole "Received Knapsack response"
         _archiver +! Message.Compose this  { ArchiveMessage.ArchiveFilePath = initialState.Configuration.ArchiveFilePath; ArchiveMessage.Files = files; }
         initialState
   
   let HandleArchiveResponse (response : ArchiveResponse) initialState =
      PrintToConsole "Received Archiver response"
      let backedUpFiles = Seq.append initialState.ProcessedFiles (response.BackedUpFiles |> Seq.map (fun tuple -> fst tuple))
      _fileManifestWriter +! Message.Compose this (WriteManifest(initialState.Configuration.ArchiveFilePath, response.BackedUpFiles |> Map.ofSeq))
      _continuationManager +! Message.Compose this { AllFiles = initialState.AllFiles; BackedUpFiles = backedUpFiles; ArchiveResponse = response }
      { initialState with ProcessedFiles = backedUpFiles }
      
   let HandleFileManifestWriterResponse response initialState =
      PrintToConsole "Received File Manifest Writer response"
      match response with 
      | FileManifestWriterResponse.Success ->
         PrintToConsole "Successfully wrote the file manifest to the archive"
         
      | FileManifestWriterResponse.Failure ->
         PrintToConsole "Failed to write the file manifest to the archive. Aborting"
         parent +! Message.Compose this BackupResponse.Failure

      initialState

   let HandleBackupContinuationResponse response initialState =
      match response with
      | Finished -> 
         PrintToConsole "Received Finished message"
         parent +! Message.Compose this BackupResponse.Success
         initialState
      | Abort -> 
         PrintToConsole "Received Abort message"
         parent +! Message.Compose this BackupResponse.Failure
         initialState
      | IgnoreFiles(files) -> 
         PrintToConsole <| sprintf "Received IgnoreFiles message. Ignoring %A" files
         { initialState with ProcessedFiles = Seq.append initialState.ProcessedFiles files }
      | ContinueProcessing -> 
         PrintToConsole "Received ContinueProcessing message"
         _volumeSwitcher +! Message.Compose this (SwitchVolumes(initialState.Configuration.ArchiveFilePath))
         initialState
   
   let HandleVolumeSwitcherResponse response (initialState : BackupManagerState) =
      match response with 
      | VolumePath(volumePath) -> 
         let newConfiguration = { initialState.Configuration with ArchiveFilePath = volumePath }
         let filesToBackup = Set.difference (Set.ofSeq initialState.AllFiles) (Set.ofSeq initialState.ProcessedFiles)
         _knapsackSolver +! Message.Compose this (Calculate(initialState.Configuration.ArchiveFilePath, filesToBackup))
         { initialState with Configuration = newConfiguration }

   (* Public Methods *)
   override this.Receive sender msg state = 
      PrintToConsole "Received initial message. Kicking off FileChooser"
      _fileChooser +! Message.Compose this (ChooseFiles(state.Configuration))
      state

   override this.PreStart() = { AllFiles = Seq.empty; ProcessedFiles = Seq.empty; Configuration = initialConfig }

   member private this.ShutdownChildren() =
      PrintToConsole "Shutting down Backup Manager"
      _archiver +! Message.Compose this Die 
      _continuationManager +! Message.Compose this Die
      _fileChooser +! Message.Compose this Die
      _knapsackSolver +! Message.Compose this Die
      _fileManifestWriter +! Message.Compose this Die
      _archiveResolver +! Message.Compose this Die

   override this.UnknownMessageHandler sender msg initialState =
      match msg with
      | :? FileChooserResponse as response -> HandleFileChooserResponse response initialState
      | :? KnapsackResponse as response -> HandleKnapsackMessage response initialState
      | :? ArchiveResponse as response -> HandleArchiveResponse response initialState
      | :? ArchiveResolverResponse as response -> HandleArchiveResolverResponse response initialState
      | :? BackupContinuationResponse as response -> HandleBackupContinuationResponse response initialState
      | :? VolumeSwitcherResponse as response -> HandleVolumeSwitcherResponse response initialState
      | _ -> initialState