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
open Microsoft.FSharp.Collections
open System

///<summary>Represents the BackupActor's state
type BackupActorState = { 
      FilesBackedUp : Set<String>; 
      BackupActor : IActor option;
      KnapsackActor : IActor option;
      BackupErrorActor : IActor option;
   }
 
///<summary>The main actor in charge of backing up the system</summary>
type BackupManager(parent : IActor) =
   inherit ActorBase<BackupMessage, BackupActorState>(parent)

   (* Private Methods *)
   member private this.BackupFiles state = state
   
   member private this.HandleArchiveResponse msg state =
      state

   member private this.HandleFileChooserResponse msg state =
      state

   member private this.HandleKnapsackResponse msg state =
      state


   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with 
      | SetArchiveActor(actor) -> { state with BackupActor = Some(actor) }
      | SetKnapsackActor(actor) -> { state with KnapsackActor = Some(actor) }
      | SetBackupErrorActor(actor) -> { state with BackupErrorActor = Some(actor) }
      | Start -> this.BackupFiles state

   override this.PreStart() = { FilesBackedUp = Set.empty; BackupActor = None; KnapsackActor = None; BackupErrorActor = None }

   override this.UnknownMessageHandler msg initialState =
      match msg with
      | :? ArchiveResponse as msg -> initialState
      | :? FileChooserResponse as msg -> initialState
      | :? KnapsackResponse as msg -> initialState
      | _ -> initialState