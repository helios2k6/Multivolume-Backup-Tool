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

open Actors
open MBT.Core
open Newtonsoft.Json
open System.Collections.Generic
open System.IO

type internal ManifestProcessor() =
   inherit BaseStatelessActor()

   (* Private methods *)
   let tryDeserializeManifestFile fileContents =
      try
         ConsoleActor.Instance.WriteLine "Deserializing file manifest"
         JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents)
         |> Seq.map (|KeyValue|)
         |> Map.ofSeq
         |> Some
      with
         | ex -> 
            ConsoleActor.Instance.WriteLine <| sprintf "Could not deserialize file manifest. Reason: %A" ex
            None

   let tryReadManifestFile archiveFilePath =
      let pathOfManifestFile = Path.Combine(archiveFilePath, Constants.FileManifestFileName)
      if File.Exists(pathOfManifestFile) then
         File.ReadAllText(pathOfManifestFile) |> tryDeserializeManifestFile
      else
         None

   let processActorMessage (msg : ActorMessage<string>) =
      let archiveFilePath = msg.Payload
      let sender = msg.Sender
      let manifestFileOption = tryReadManifestFile archiveFilePath
      
      match manifestFileOption with
      | Some(fileManifest) -> 
      | None -> 

      ()

   override this.ProcessStatelessMessage msg =
      match msg with
      | ManifestProcessorMessage(actorMessage) -> processActorMessage actorMessage
      | _ -> failwith "Unknown message"
      ()