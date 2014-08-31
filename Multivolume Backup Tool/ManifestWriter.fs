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
/// Serialization object for a file manifest
/// </summary>
[<JsonObject(MemberSerialization.OptIn)>]
type ManifestInfo() =
   /// <summary>
   /// The mapping between live files and storage files
   /// </summary>
   [<JsonProperty("LiveToStorage")>]
   member val LiveToStorage = new Dictionary<string, string>() with get, set
   /// <summary>
   /// The mapping between storage files and live files
   /// </summary>
   [<JsonProperty("StorageToLive")>]
   member val StorageToLive = new Dictionary<string, string>() with get, set

/// <summary>
/// Actor in charge of writing the file manifest
/// </summary>
type internal ManifestWriter() = 
   inherit BaseStatelessActor()

   (* Private methods *)
   let tryDeserializeManifestInfo fileContents =
      try
         Some <| JsonConvert.DeserializeObject<ManifestInfo>(fileContents)
      with
         | ex -> 
            puts <| sprintf "Could not deserialize file manifest. Reason: %A" ex
            None

   let tryReadManifestFile archiveFilePath =
      let pathOfManifestFile = Path.Combine(archiveFilePath, Constants.FileManifestFileName)
      if File.Exists pathOfManifestFile then
         File.ReadAllText pathOfManifestFile |> tryDeserializeManifestInfo
      else
         None
   
   let mergeManifestAndStorageReport (existingManifest : ManifestInfo) storageReport =
      let iterAction live storage =
         let oldLiveToStorageResult, oldLiveToStorage = existingManifest.LiveToStorage.TryGetValue live
         let oldStorageToLiveResult, oldStorageToLive = existingManifest.StorageToLive.TryGetValue storage

         if oldLiveToStorageResult then existingManifest.LiveToStorage.Remove oldLiveToStorage |> ignore
         if oldStorageToLiveResult then existingManifest.StorageToLive.Remove oldStorageToLive |> ignore

         existingManifest.LiveToStorage.[live] <- storage
         existingManifest.StorageToLive.[storage] <- live

      Map.iter iterAction storageReport
   
   let createManifestInfo archivePath storageReport = 
      let existingManifestOption = tryReadManifestFile archivePath

      match existingManifestOption with
      | Some(manifestInfo) -> 
         mergeManifestAndStorageReport manifestInfo storageReport
         manifestInfo
      | _ -> 
         let manifestInfo = new ManifestInfo()
         manifestInfo.LiveToStorage <- Map.convertToDictionary storageReport
         manifestInfo.StorageToLive <- Map.convertToDictionary << Map.reverse <| storageReport
         manifestInfo

   let tryWriteManifestFile archivePath storageReport =
      try
         let manifestInfo = createManifestInfo archivePath storageReport
         let serializedContext = JsonConvert.SerializeObject(manifestInfo, Formatting.Indented)
         let manifestFilePath = Path.Combine(archivePath, Constants.FileManifestFileName)

         File.WriteAllText(manifestFilePath, serializedContext)
         true
      with
         | _ -> false

   let processMessage actorMessage = 
      match actorMessage.Callback with
      | Some(callback) -> 
         let storageReport = actorMessage.Payload.StorageReport
         let archivePath = actorMessage.Payload.RootArchivePath

         let outputResult = tryWriteManifestFile archivePath storageReport
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