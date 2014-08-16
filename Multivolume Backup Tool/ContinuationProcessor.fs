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

type internal ContinuationProcessor() =
   inherit BaseStatelessActor()

   (* Private methods *)
   let calculateAllArchivedFiles knownArchivedFiles manifest = Map.keys manifest |> Seq.append knownArchivedFiles

   let calculateRemainingFiles allFiles knownArchivedFiles = 
      let allFilesSet = Set.ofSeq allFiles
      let allKnownArchivedFiles = Set.ofSeq knownArchivedFiles

      allFilesSet - allKnownArchivedFiles

   let processMessage actorMessage = 
      match actorMessage.Callback with
      | Some(callback) -> 
         let payload = actorMessage.Payload
         let allArchivedFiles = calculateAllArchivedFiles payload.KnownBackedUpFiles payload.LatestManifest
         let remaining = calculateRemainingFiles payload.AllFiles allArchivedFiles
         
         ResponseMessage.Continuation { Archived = allArchivedFiles; Remaining = remaining } |> callback
      | _ -> failwith "Unable to callback"

   (* Public methods *)
   override this.ProcessStatelessMessage msg = 
      match msg with 
      | Continuation(actorMessage) -> processMessage actorMessage
      | _ -> failwith "Unknown message"