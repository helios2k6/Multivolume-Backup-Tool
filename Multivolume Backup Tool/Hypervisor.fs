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
open System

type private BackupStatus = 
   | AwaitingStart
   | InProgress
   | Finished
   | Failed

type private HypervisorState = { Status : BackupStatus }

type private ExternalRequest =
   | Start
   | Wait of AsyncReplyChannel<unit>
   | Stop

type private HypervisorInternalMessage = 
   | External of ExternalRequest
   | Internal of BackupResponse

type Hypervisor(appConfig : ApplicationConfiguration) =
   (* Private Fields *)
   member private this._mailbox = MailboxProcessor.Start this.MessageLoop

   (* Private Methods *)
   member private this.HandleExternalRequest request =
      match request with
      | Start -> ()
      | Wait(replyChannel) -> ()
      | Stop -> ()

   member private this.MessageLoop (inbox : MailboxProcessor<HypervisorInternalMessage>) =
      let rec loop state = 
         async {
            let! msg = inbox.Receive()

            match msg with
            | External(request) -> ()
            | Internal(response) -> return! loop state
         }
      loop { Status = AwaitingStart }

   interface IActor with
      member this.Post msg = 
         match msg with
         | :? HypervisorInternalMessage as msg -> this._mailbox.Post msg
         | _ -> ()
   end