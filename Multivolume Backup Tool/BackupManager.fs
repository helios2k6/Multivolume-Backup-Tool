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

open MBT.Messages
open MBT.Operations
open Microsoft.FSharp.Collections
open System

///<summary>Represents the BackupActor's state
type BackupActorState = 
   {
      ApplicationConfiguration : ApplicationConfiguration option;
      FilesBackedUp : seq<String>;
      FilesToBackUp : seq<String>;
      Archiver : IActor option;
      KnapsackSolver : IActor option;
      BackupErrorHandler : IActor option;
      FileChooser : IActor option;
   }
 
///<summary>The main actor in charge of backing up the system</summary>
type BackupManager(parent : IActor) =
   inherit ActorBase<BackupMessage, BackupActorState>(parent)

   (* Private Methods *)
   member private this.BackupFiles state = state
   
   member private this.HandleArchiveResponse (msg : ArchiveResponse) (state : BackupActorState) =
      match state with
      | { BackupErrorHandler = errorHandlerOpt; FilesToBackUp = filesToBackUp } ->
         match errorHandlerOpt with
         | Some(errorHandler) ->
               match msg with
               | { ArchiveResponse.Result = result; ArchiveResponse.OriginalMessage = originalMessage } -> 
                  match result with
                  | ArchiveResult.Success -> 
                     parent +! { Sender = this; Payload = BackupResponse.Success }
                     //TODO: GOTTA MAKE SURE WE ACTUALLY FINISH ARCHIVING THE REST OF THE FILES
                     { state with FilesBackedUp = (Seq.append state.FilesBackedUp originalMessage.Files) }
                  | ArchiveResult.UnableToOpenArchiveFilePath(str) -> 
                     errorHandler +! { Sender = this; Payload = CannotOpenArchiveFilePath(str) }
                     state
                  | ArchiveResult.UnableToOpenSourceFile(str) -> 
                     errorHandler +! { Sender = this; Payload = CannotCopyFileToArchivePath(str) }
                     state
                  | ArchiveResult.UnknownError(ex) -> 
                     parent +! { Sender = this; Payload = FailureAbort(Some(ex)) }
                     state
         | None -> 
            parent +! { Sender = this; Payload = FailureBackupErrorHandlerNotSet }
            state

   member private this.HandleFileChooserResponse (msg : FileChooserResponse) (state : BackupActorState) =
      match state with 
      | { KnapsackSolver = solverOpt; ApplicationConfiguration = configOpt } ->
         match solverOpt with
         | Some(solver) -> 
            match configOpt with
            | Some(config) ->
               match msg with
               | Files(files) -> solver +! { Sender = this; Payload = Calculate(config, files) }
            | None -> parent +! { Sender = this; Payload = FailureArchiveFilePathNotSet }
         | None -> parent +! { Sender = this; Payload = FailureKnapsackSolverNotSet }
      state

   member private this.HandleKnapsackResponse (msg : KnapsackResponse) (state : BackupActorState) =
      state

   member private this.HandleBackupErrorHandlerResponse (msg : BackupErrorHandlerResponse) (state : BackupActorState) =
      state

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with 
      | SetArchiveActor(actor) -> { state with Archiver = Some(actor) }
      | SetKnapsackActor(actor) -> { state with KnapsackSolver = Some(actor) }
      | SetBackupErrorActor(actor) -> { state with BackupErrorHandler = Some(actor) }
      | SetFileChooser(actor) -> { state with FileChooser = Some(actor) }
      | SetArchiveFilePath(path) -> { state with ArchiveFilePath = Some(path) }
      | Start -> this.BackupFiles state

   override this.PreStart() = 
      { 
         ArchiveFilePath = None; 
         FilesBackedUp = Seq.empty;
         FilesToBackUp = Seq.empty; 
         Archiver = None; 
         KnapsackSolver = None; 
         BackupErrorHandler = None;
         FileChooser = None;
      }

   override this.UnknownMessageHandler msg initialState =
      match msg with
      | :? ArchiveResponse as msg -> this.HandleArchiveResponse msg initialState
      | :? FileChooserResponse as msg -> this.HandleFileChooserResponse msg initialState
      | :? KnapsackResponse as msg -> this.HandleKnapsackResponse msg initialState
      | :? BackupErrorHandlerResponse as msg -> this.HandleBackupErrorHandlerResponse msg initialState
      | _ -> initialState