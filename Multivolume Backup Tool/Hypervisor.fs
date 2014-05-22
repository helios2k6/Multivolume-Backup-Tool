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
open Microsoft.FSharp.Control
open log4net
open System

type private Callback = unit -> unit

type private HypervisorState = 
   | InitialState 
   | StartState 
   | WaitingState of Callback
   | FinishedState of BackupResponse
   | ShutdownState

type private ExternalRequest = 
   | Start
   | Wait of Callback
   | Shutdown

type private HypervisorMessage = 
   | Internal of BackupResponse
   | External of ExternalRequest
   
type Hypervisor(appConfig : ApplicationConfiguration) as this =
   static let Log = LogManager.GetLogger(typedefof<Hypervisor>)

   static let (|IsShutdownState|) state =
      match state with
      | ShutdownState -> true
      | _ -> false

   (* Private Fields *)
   let _backupManager = new BackupManager(this, appConfig)
   let _mailbox = MailboxProcessor.Start this.InternalMessageLoop

   (* Private Methods *)
   member private this.HandleInitialStateMessage msg state =
      match msg with
      | External(request) -> 
         match request with
         | Start -> 
            Log.Info "Kicking off BackupManager"
            _backupManager +! { Sender = this; Payload = BackupMessage.Start }
            Log.Info "Moving to Start State"
            StartState
         | Shutdown -> 
            Log.Info "Moving to Shutdown State"
            ShutdownState
         | _ -> state
      | _ -> state

   member private this.HandleStartStateMessage msg state = 
      match msg with
      | External(request) ->
         match request with
         | Wait(callback) -> 
            Log.Info "Moving to Waiting State"
            WaitingState(callback)
         | _ -> state
      | Internal(response) -> 
         Log.Info "Moving to Finished State"
         FinishedState(response)

   member private this.HandleWaitingStateMessage msg state = 
      match msg with
      | Internal(response) -> 
         match state with
         | WaitingState(callback) -> 
            Log.Info "Executing callback"
            callback()
            Log.Info "Moving to Finished State"
            FinishedState(response)
         | _ -> state
      | _ -> state

   member private this.HandleFinishedStateMessage msg state = 
      match msg with
      | External(request) -> 
         match request with
         | Wait(callback) -> 
            Log.Info "Executing callback"
            callback()
            Log.Info "Moving to Shutdown State"
            ShutdownState
         | Shutdown -> 
            Log.Info "Moving to Shutdown State"
            ShutdownState
         | _ -> state
      | _ -> state

   member private this.ShutdownBackupManager() = _backupManager +! { Sender = this; Payload = Die }

   member private this.InternalMessageLoop (inbox : MailboxProcessor<HypervisorMessage>) = 
      let rec loop state =
         async {
            if (|IsShutdownState|) state then this.ShutdownBackupManager()
            else
               let! msg = inbox.Receive()

               match state with
               | InitialState -> return! this.HandleInitialStateMessage msg state |> loop 
               | StartState -> return! this.HandleStartStateMessage msg state |> loop 
               | WaitingState(callback) -> return! this.HandleWaitingStateMessage msg state |> loop 
               | FinishedState(result) -> return! this.HandleFinishedStateMessage msg state |> loop
               | ShutdownState -> this.ShutdownBackupManager()
         }

      loop InitialState

   (* Public Methods *)
   member public this.Begin() = 
      Log.Info "Starting Hypervisor"
      Start |> (this :> IActor).Post

   member public this.Wait (callback : (unit -> unit)) = 
      Log.Info "Setting wait callback on Hypervisor"
      Wait(callback) |> (this :> IActor).Post

   member public this.Shutdown() = 
      Log.Info "Shutting down Hypervisor"
      Shutdown |> (this :> IActor).Post

   interface IActor with
      member this.Post msg = 
         match msg with
         | :? Message as message -> 
            match message.Payload with
            | :? BackupResponse as response -> Internal(response) |> _mailbox.Post
            | _ -> ()
         | :? ExternalRequest as request -> External(request) |> _mailbox.Post
         | _ -> ()
   end 