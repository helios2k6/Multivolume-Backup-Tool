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
   let _continuationManager = new BackupContinuationManager(this)
   let _volumeSwitcher = new VolumeSwitcher(this)
   
   (* Private Methods *)
   ///<summary>This handles the file chooser response. It will forward the messages to the knapsack actor</summary>
   member private this.HandleFileChooserResponse response initialState =
      match response with
      | FileChooserResponse.Files(files) -> 
         PrintToConsole "Received FileChooser response. Messaging KnapsackSolver"
         _knapsackSolver +! Message.Compose this (Calculate(initialState.Configuration.ArchiveFilePath, files))
         PrintToConsole "Adding all files that need to be backed up"
         { initialState with BackupManagerState.AllFiles = Seq.cache files }
      | FileChooserResponse.Failure -> 
         PrintToConsole "Unable to choose files due to error"
         parent +! Message.Compose this BackupResponse.Failure
         initialState
   
   member private this.HandleKnapsackMessage response initialState =
      match response with
      | KnapsackResponse.Files(files) -> 
         PrintToConsole "Received Knapsack response. Messaging Archiver"
         _archiver +! Message.Compose this  { ArchiveMessage.ArchiveFilePath = initialState.Configuration.ArchiveFilePath; ArchiveMessage.Files = files; }
         initialState
   
   member private this.HandleArchiveResponse (response : ArchiveResponse) initialState =
      PrintToConsole "Received Archiver response. Messaging ContinuationManager"
      let backedUpFiles = Seq.append initialState.ProcessedFiles response.BackedUpFiles
      _continuationManager +! Message.Compose this { AllFiles = initialState.AllFiles; BackedUpFiles = backedUpFiles; ArchiveResponse = response }
      { initialState with ProcessedFiles = backedUpFiles }

   member private this.HandleBackupContinuationResponse response initialState =
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
   
   member private this.HandleVolumeSwitcherResponse response (initialState : BackupManagerState) =
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

   override this.PreShutdown state =
      PrintToConsole "Shutting down children"
      _archiver +! Message.Compose this Die 
      _continuationManager +! Message.Compose this Die
      _fileChooser +! Message.Compose this Die
      _knapsackSolver +! Message.Compose this Die

   override this.UnknownMessageHandler sender msg initialState =
      match msg with
      | :? FileChooserResponse as response -> this.HandleFileChooserResponse response initialState
      | :? KnapsackResponse as response -> this.HandleKnapsackMessage response initialState
      | :? ArchiveResponse as response -> this.HandleArchiveResponse response initialState
      | :? BackupContinuationResponse as response -> this.HandleBackupContinuationResponse response initialState
      | :? VolumeSwitcherResponse as response -> this.HandleVolumeSwitcherResponse response initialState
      | _ -> initialState