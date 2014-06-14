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
open Microsoft.FSharp.Control
open System

type private Callback = unit -> unit

type private HypervisorState = 
   | Initialized
   | Started
   | Waiting of Callback
   | ShuttingDown of Callback Option
   | Shutdown

type private ExternalRequest = 
   | Start
   | Wait of Callback
   | Shutdown

type private InternalRequest =
   | BackupResponse of BackupResponse
   | ShutdownResponse of ShutdownResponse

type private HypervisorMessage = 
   | Internal of InternalRequest
   | External of ExternalRequest
   
type Hypervisor(appConfig : ApplicationConfiguration) as this =
   static let IsShutdownState (state : HypervisorState) =
      match state with
      | HypervisorState.Shutdown -> true
      | _ -> false

   (* Private Fields *)
   let _backupManager = new BackupManager(this, appConfig)
   let _mailbox = MailboxProcessor.Start this.InternalMessageLoop

   (* Private Methods *)
   let (|GetInternalResponse|_|) msg =
      match msg with
      | Internal(response) -> Some response
      | _ -> None

   let (|GetExternalRequest|_|) msg =
      match msg with
      | External(request) -> Some request
      | _ -> None

   let (|GetShutdownResponse|_|) msg =
      match msg with
      | ShutdownResponse(response) -> Some response
      | _ -> None

   let (|InternalShutdownResponse|_|) msg =
      match msg with
      | GetInternalResponse (GetShutdownResponse response) -> Some response
      | _ -> None

   let (|ShuttingDownCallback|_|) state =
      match state with
      | ShuttingDown(callbackOpt) -> callbackOpt
      | _ -> None
   
   let (|GetBackupResponse|_|) msg =
      match msg with
      | BackupResponse(response) -> Some response
      | _ -> None

   let (|GetWaitingCallback|_|) state =
      match state with
      | Waiting(callback) -> Some callback
      | _ -> None

   let (|GetWaitRequest|_|) req =
      match req with
      | Wait(callback) -> Some callback
      | _ -> None

   let PrintResponseResults response = 
      match response with
      | Success -> PrintToConsole "Backup process was a success"
      | Failure -> PrintToConsole "Backup process was a failure"

   let HandleInitializedStateMessage msg state =
      match msg with
      | External(request) -> 
         match request with
         | Start -> 
            _backupManager +! Message.Compose this BackupMessage.Start
            Started
         | Shutdown -> ShuttingDown(None)
         | _ -> state
      | _ -> state

   let HandleStartedStateMessage msg state =
      match msg with
      | GetExternalRequest (GetWaitRequest callback) -> Waiting callback
      | GetInternalResponse (GetBackupResponse response) -> 
         PrintResponseResults response
         this.ShutdownBackupManager()
         ShuttingDown None
      | _ -> state

   let HandleWaitingStateMessage msg state =
      match msg with
      | GetInternalResponse (GetBackupResponse response) ->
         PrintResponseResults response
         match state with
         | GetWaitingCallback callback -> 
            this.ShutdownBackupManager()
            ShuttingDown (Some callback)
         | _ -> state
      | _ -> state

   let HandleShuttingDownStateMessage msg state =
      match msg with
      | InternalShutdownResponse _ ->
         match state with
         | ShuttingDownCallback callback -> callback()
         | _ -> ()
         HypervisorState.Shutdown
      | _ -> state

   member private this.ShutdownBackupManager() = _backupManager +! Message.Compose this Die

   member private this.InternalMessageLoop (inbox : MailboxProcessor<HypervisorMessage>) = 
      let rec loop (state : HypervisorState) =
         async {
            let! msg = inbox.Receive()

            match state with
            | Initialized -> return! HandleInitializedStateMessage msg state |> loop
            | Started -> return! HandleStartedStateMessage msg state |> loop
            | Waiting(callback) -> return! HandleWaitingStateMessage msg state |> loop
            | ShuttingDown(callbackOpt) -> HandleShuttingDownStateMessage msg state |> ignore
            | HypervisorState.Shutdown -> ()
         }

      loop Initialized

   (* Public Methods *)
   member public this.Begin() = Start |> (this :> IActor).Post

   member public this.Wait (callback : (unit -> unit)) = Wait(callback) |> (this :> IActor).Post

   member public this.Shutdown() = Shutdown |> (this :> IActor).Post

   interface IActor with
      member this.Post msg = 
         match msg with
         | :? Message as message -> 
            match message.Payload with
            | :? BackupResponse as response -> Internal(BackupResponse(response)) |> _mailbox.Post
            | :? ShutdownResponse as response -> Internal(ShutdownResponse(response)) |> _mailbox.Post
            | _ -> ()
         | :? ExternalRequest as request -> External(request) |> _mailbox.Post
         | _ -> ()
   end 