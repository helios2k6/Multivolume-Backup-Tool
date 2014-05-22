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
open MBT.Messages
open MBT.Operations
open Microsoft.FSharp.Collections
open log4net
open System

///<summary>The backup manager's state object</summary>
type BackupManagerState = { AllFiles : seq<String>; ProcessedFiles : seq<String>; }

///<summary>The main actor in charge of backing up the system</summary>
type BackupManager(parent : IActor, config : ApplicationConfiguration) as this =
   inherit ActorBase<BackupMessage, BackupManagerState>(parent)

   (* Private Static Fields *)
   static let Log = LogManager.GetLogger(typedefof<BackupManager>)

   (* Private Fields *)
   let _fileChooser = new FileChooser(this)
   let _knapsackSolver = new KnapsackSolver(this)
   let _archiver = new Archiver(this)
   let _continuationManager = new BackupContinuationManager(this)
   
   (* Private Methods *)
   ///<summary>This handles the file chooser response. It will forward the messages to the knapsack actor</summary>
   member private this.HandleFileChooserResponse response initialState =
      match response with
      | FileChooserResponse.Files(files) -> 
         Log.Info "Received FileChooser response. Messaging KnapsackSolver"
         _knapsackSolver +! { Sender = this; Payload = Calculate(config.ArchiveFilePath, files) }
         Log.Info "Adding all files that need to be backed up"
         { initialState with BackupManagerState.AllFiles = Seq.cache files }
   
   member private this.HandleKnapsackMessage response initialState =
      match response with
      | KnapsackResponse.Files(files) -> 
         Log.Info "Received Knapsack response. Messaging Archiver"
         _archiver +! { Sender = this; Payload = { ArchiveMessage.ArchiveFilePath = config.ArchiveFilePath; ArchiveMessage.Files = files; } }
         initialState
   
   member private this.HandleArchiveResponse (response : ArchiveResponse) initialState =
      Log.Info "Received Archiver response. Messaging ContinuationManager"
      let backedUpFiles = Seq.append initialState.ProcessedFiles response.BackedUpFiles
      _continuationManager +! { Sender = this; Payload = { AllFiles = initialState.AllFiles; BackedUpFiles = backedUpFiles; ArchiveResponse = response } }
      { initialState with ProcessedFiles = backedUpFiles }

   member private this.HandleBackupContinuationResponse response initialState =
      match response with
      | Finished -> 
         Log.Info "Received Finished message"
         parent +! { Sender = this; Payload = BackupResponse.Success }
         initialState
      | Abort -> 
         Log.Info "Received Abort message"
         parent +! { Sender = this; Payload = BackupResponse.Failure }
         initialState
      | IgnoreFiles(files) -> 
         Log.Info <| sprintf "Received IgnoreFiles message. Ignoring %A" files
         { initialState with ProcessedFiles = Seq.append initialState.ProcessedFiles files }
      | ContinueProcessing -> 
         Log.Info "Received ContinueProcessing message"
         let filesToBackup = Set.difference (Set.ofSeq initialState.AllFiles) (Set.ofSeq initialState.ProcessedFiles)
         _knapsackSolver +! { Sender = this; Payload = Calculate(config.ArchiveFilePath, filesToBackup) }
         initialState

   (* Public Methods *)
   override this.Receive sender msg state = 
      Log.Info "Received initial message. Kicking off FileChooser"
      _fileChooser +! { Sender = this; Payload = ChooseFiles(config) }
      state

   override this.PreStart() = { AllFiles = Seq.empty; ProcessedFiles = Seq.empty }

   override this.PreShutdown state =
      Log.Info "Shutting down children"
      _archiver +! { Sender = this; Payload = Die } 
      _continuationManager +! { Sender = this; Payload = Die } 
      _fileChooser +! { Sender = this; Payload = Die } 
      _knapsackSolver +! { Sender = this; Payload = Die } 

   override this.UnknownMessageHandler sender msg initialState =
      match msg with
      | :? FileChooserResponse as response -> this.HandleFileChooserResponse response initialState
      | :? KnapsackResponse as response -> this.HandleKnapsackMessage response initialState
      | :? ArchiveResponse as response -> this.HandleArchiveResponse response initialState
      | :? BackupContinuationResponse as response -> this.HandleBackupContinuationResponse response initialState
      | _ -> initialState