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
open System.IO

type ActorStatus = Started | ShuttingDown | Shutdown

///<summary>The backup manager's state object</summary>
type BackupManagerState = 
   { 
      AllFiles : FileInfo list; 
      ProcessedFiles : FileInfo list;
      Configuration : ApplicationConfiguration; 
      Status : ActorStatus; 
      ChildrenStatus : (IActor * ActorStatus) list;
      FileManifest : FileManifest Option
   }

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
      match response with
      | FileChooserResponse.Files(files) -> 
         _archiveResolver +! Message.Compose this { ArchiveResolverMessage.ArchiveFilePath = initialState.Configuration.ArchiveFilePath; Files = files; }
         Some { initialState with AllFiles = files }
      | FileChooserResponse.Failure -> 
         parent +! Message.Compose this BackupResponse.Failure
         None

   let HandleArchiveResolverResponse (response : ArchiveResolverResponse) initialState =
      let processedFiles = response.FileManifest |> Seq.map (fun tuple -> tuple.Key) |> Set.ofSeq
      let allFilesAsSet = Set.ofSeq response.Files
      let filesToProcess = allFilesAsSet - processedFiles |> Seq.toList
      
      _knapsackSolver +! Message.Compose this (KnapsackMessage.Calculate(initialState.Configuration.ArchiveFilePath, filesToProcess))
      Some { initialState with ProcessedFiles = processedFiles |> Seq.toList; FileManifest = Some response.FileManifest }

   let HandleKnapsackMessage response initialState =
      match response with
      | KnapsackResponse.Files(files) -> 
         _archiver +! Message.Compose this  { ArchiveMessage.ArchiveFilePath = initialState.Configuration.ArchiveFilePath; ArchiveMessage.Files = files; }
         Some initialState
   
   let HandleArchiveResponse (response : ArchiveResponse) initialState =
      let responseManifest = response.BackedUpFiles |> Map.ofSeq

      let createNewFileManifest oldManifest responseManifest =
         let foldAction stateMap key value = Map.add key value stateMap
         Map.fold foldAction oldManifest responseManifest
      
      let backedUpFiles = initialState.ProcessedFiles @ (response.BackedUpFiles |> List.map (fun tuple -> fst tuple))
      
      let notifyAgents refreshedManifest =
         _fileManifestWriter +! Message.Compose this (WriteManifest(initialState.Configuration.ArchiveFilePath, refreshedManifest))
         _continuationManager +! Message.Compose this { AllFiles = initialState.AllFiles; BackedUpFiles = backedUpFiles; ArchiveResponse = response }

      let allProcessedFiles = initialState.ProcessedFiles @ backedUpFiles

      match initialState.FileManifest with
      | Some(oldFileManifest) -> 
         let refreshedManifest = createNewFileManifest oldFileManifest responseManifest
         notifyAgents refreshedManifest
         Some { initialState with ProcessedFiles = allProcessedFiles; FileManifest = Some refreshedManifest }
      | None ->
         notifyAgents responseManifest
         Some { initialState with ProcessedFiles = allProcessedFiles; FileManifest = Some responseManifest }
      
   let HandleFileManifestWriterResponse response initialState =
      match response with 
      | FileManifestWriterResponse.Success -> parent +! Message.Compose this BackupResponse.Success
      | FileManifestWriterResponse.Failure ->
         PrintToConsole "Failed to write the file manifest to the archive. Aborting"
         parent +! Message.Compose this BackupResponse.Failure

      // Clearing out the old file manifest
      Some { initialState with BackupManagerState.FileManifest = None }

   let HandleBackupContinuationResponse response initialState =
      match response with
      | Finished -> 
         parent +! Message.Compose this BackupResponse.Success
         Some initialState
      | Abort -> 
         PrintToConsole "Aborting backup process"
         parent +! Message.Compose this BackupResponse.Failure
         Some initialState
      | IgnoreFiles(files) -> Some { initialState with ProcessedFiles = initialState.ProcessedFiles @ files }
      | ContinueProcessing -> 
         _volumeSwitcher +! Message.Compose this (SwitchVolumes(initialState.Configuration.ArchiveFilePath))
         Some initialState
   
   let HandleVolumeSwitcherResponse response (initialState : BackupManagerState) =
      match response with 
      | VolumePath(volumePath) -> 
         PrintToConsole "Continuing backup procedure"
         let newConfiguration = { initialState.Configuration with ArchiveFilePath = volumePath }
         let filesToBackup = (Set.ofList initialState.AllFiles) - (Set.ofList initialState.ProcessedFiles) |> Seq.toList
         _knapsackSolver +! Message.Compose this (Calculate(initialState.Configuration.ArchiveFilePath, filesToBackup))
         Some { initialState with Configuration = newConfiguration }

   let ShutdownChildren() =
      _archiver +! Message.Compose this Die 
      _continuationManager +! Message.Compose this Die
      _fileChooser +! Message.Compose this Die
      _knapsackSolver +! Message.Compose this Die
      _fileManifestWriter +! Message.Compose this Die
      _archiveResolver +! Message.Compose this Die
   
   let GetInitialChildrenStatusSeq() =
      [
         yield (_archiver :> IActor, Started)
         yield (_continuationManager :> IActor, Started)
         yield (_fileChooser :> IActor, Started)
         yield (_knapsackSolver :> IActor, Started)
         yield (_fileManifestWriter :> IActor, Started)
         yield (_archiveResolver :> IActor, Started)
      ]

   let QueryForChild (child : IActor) (statusBoard : (IActor * ActorStatus) seq) =
      query {
         for tuple in statusBoard do
         where (Object.ReferenceEquals((fst tuple), child))
         select (fst tuple)
         exactlyOne
      }

   let AreAllChildrenShutdown statusBoard = statusBoard |> Seq.All (fun item -> snd item = Shutdown)

   let HandleShutdownResponse sender state =
      let childQuery = QueryForChild sender state.ChildrenStatus
      let validStatuses = state.ChildrenStatus |> List.filter (fun element -> not <| Object.Equals(fst element, childQuery))
      let updatedStatusBoard = (childQuery, Shutdown) :: validStatuses
      
      //Check to see if everyone is shutdown
      if AreAllChildrenShutdown updatedStatusBoard then 
         parent +! Message.Compose this ShutdownResponse.Finished
         None
      else Some { state with ChildrenStatus = updatedStatusBoard }

   (* Public Methods *)
   override this.Receive sender msg state = 
      _fileChooser +! Message.Compose this (ChooseFiles(state.Configuration))
      Some state

   override this.PreStart() = 
      { 
         AllFiles = List.empty;
         ProcessedFiles = List.empty; 
         Configuration = initialConfig; 
         Status = Started; 
         ChildrenStatus = GetInitialChildrenStatusSeq();
         FileManifest = None;
      }

   override this.UnknownMessageHandler sender msg initialState =
      let currentStatus = initialState.Status
      match currentStatus with
      | Started ->
         match msg with
         | :? FileChooserResponse as response -> HandleFileChooserResponse response initialState
         | :? ArchiveResolverResponse as response -> HandleArchiveResolverResponse response initialState
         | :? KnapsackResponse as response -> HandleKnapsackMessage response initialState
         | :? ArchiveResponse as response -> HandleArchiveResponse response initialState
         | :? BackupContinuationResponse as response -> HandleBackupContinuationResponse response initialState
         | :? VolumeSwitcherResponse as response -> HandleVolumeSwitcherResponse response initialState
         | _ -> Some initialState
      | ShuttingDown -> 
         match msg with
         | :? ShutdownResponse as shutdownResponse -> HandleShutdownResponse sender initialState
         | _ -> Some initialState
      | _ -> None

   override this.HandleShutdownMessage _ state =
      ShutdownChildren()
      let freshStatusBoard = state.ChildrenStatus |> List.map (fun item -> (fst item, ShuttingDown))
      Some { state with Status = ShuttingDown; ChildrenStatus = freshStatusBoard }