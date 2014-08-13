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
open Newtonsoft.Json
open System.IO

/// <summary>
/// Actor in charge of writing the file manifest
/// </summary>
type internal ManifestWriter() = 
   inherit BaseStatelessActor()

   (* Private methods *)
   let tryWriteManifestFile archivePath manifest =
      try 
         let serializedContext = JsonConvert.SerializeObject(manifest, Formatting.Indented)
         let manifestFilePath = Path.Combine(archivePath, Constants.FileManifestFileName)
         File.WriteAllText(manifestFilePath, serializedContext)
         true
      with
         | _ -> false

   let processMessage actorMessage = 
      match actorMessage.Callback with
      | Some(callback) -> 
         let manifest = actorMessage.Payload.Manifest
         let archivePath = actorMessage.Payload.RootArchivePath

         let outputResult = tryWriteManifestFile archivePath manifest
         if outputResult then
            ResponseMessage.Manifest Success |> callback
         else
            ResponseMessage.Manifest Failure |> callback

      | _ -> failwith "Unable to callback"

   (* Public methods *)
   override this.ProcessStatelessMessage msg = 
      match msg with 
      | Manifest(actorMessage) -> processMessage actorMessage
      | _ -> failwith "Unknown message"