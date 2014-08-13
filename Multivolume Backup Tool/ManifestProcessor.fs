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
open MBT.Console
open Newtonsoft.Json
open System.Collections.Generic
open System.IO

/// <summary>
/// Processes the manifest file for a given archive
/// </summary>
type internal ManifestProcessor() =
   inherit BaseStatelessActor()

   (* Private methods *)
   let tryDeserializeManifestFile fileContents =
      try
         puts "Deserializing file manifest"
         JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents)
         |> Seq.map (|KeyValue|)
         |> Map.ofSeq
         |> Some
      with
         | ex -> 
            puts <| sprintf "Could not deserialize file manifest. Reason: %A" ex
            None

   let tryReadManifestFile archiveFilePath =
      let pathOfManifestFile = Path.Combine(archiveFilePath, Constants.FileManifestFileName)
      if File.Exists pathOfManifestFile then
         File.ReadAllText pathOfManifestFile |> tryDeserializeManifestFile
      else
         None

   let processActorMessage msg =
      let manifestFileOption = tryReadManifestFile msg.Payload
      
      match msg.Callback with
      | Some(callback) -> ResponseMessage.ManifestProcessor manifestFileOption |> callback
      | _ -> failwith "Unable to callback with response"

   (* Public methods *)
   override this.ProcessStatelessMessage msg =
      match msg with
      | ManifestProcessor(actorMessage) -> processActorMessage actorMessage
      | _ -> failwith "Unknown message"