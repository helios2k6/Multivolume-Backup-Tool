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

open System
open Microsoft.FSharp.Control

///<summary>The base class for all actors</summary>
[<AbstractClass>]
type ActorBase<'msg, 'state> = 
   (* Fields *)
   member private this._mailbox = MailboxProcessor.Start (this.InputLoop (this.PreStart()))
   
   (* Private Methods *)
   member private this.InputLoop initialState (inbox : MailboxProcessor<obj>) =
      let rec loop state =
         async {
            let! msg = inbox.Receive()

            match msg with
            | :? Messages.DeathNote -> 
               this.ShutdownActor state
               return ()
            | :? Message as rMsg -> return! this.HandleMessage rMsg state |> loop
            | unkMsg -> return! this.UnknownMessageHandler unkMsg state |> loop

         }
      loop initialState

   member private this.HandleMessage message state =
      match message with
      | { Message.Sender = sender; Message.Payload = payload } -> 
         match payload with
         | :? 'msg as aMsg -> this.Receive sender aMsg state
         | unkMsg -> this.UnknownMessageHandler unkMsg state

   member private this.ShutdownActor state = this.PreShutdown state

   (* Public Methods *)
   ///<summary>Process a messge</summary>
   abstract member Receive : IActor -> 'msg -> 'state -> 'state
   
   ///<summary>The function called prior to starting up this actor</summary>
   abstract member PreStart : unit -> 'state

   ///<summary>Handles an unknown message</summary>
   abstract member UnknownMessageHandler : obj -> 'state -> 'state
   default this.UnknownMessageHandler msg initialState = initialState

   ///<summary>The function called prior to shutting down the actor</summary>
   abstract member PreShutdown : 'state -> unit
   default this.PreShutdown state = ()

   ///<summary>Shutdown this actor</summary>
   member public this.Shutdown() = (this :> IActor).Post Messages.Die

   interface IActor with
      member this.Post msg = this._mailbox.Post msg
   end