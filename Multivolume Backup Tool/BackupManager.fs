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
open Actors.ActorOperations
open MBT.Core
open MBT.Core.IO

/// <summary>
/// Represents the associated stateful info of the BackupManager 
/// </summary>
type internal Info = { AllFiles : FileEntry seq; ProcessedFiles : FileEntry seq; RemainingFiles : FileEntry seq }

/// <summary>
/// The possible states the Backup Manager can be in
/// </summary>
type internal BackupManagerState = 
   | Initial
   | Discovery
   | Solving of Info
   | Archiving of Info
   | WritingManifest of Info
   | Switching of Info
   | Finished
   | Error

/// <summary>
/// The main actor for managing backup operations
/// </summary>
type internal BackupManager(parent : IActor<BackupManagerResult>, config : ApplicationConfiguration) as this =
   inherit BaseStateActor<BackupManagerState>(Initial)

   (* Private fields *)
   let fileChooser = new FileChooser()
   let spaceSolver = new SpaceSolver()
   let archiver = new Archiver()
   let manifestWriter = new ManifestWriter()
   let volumeSwitcher = new VolumeSwitcher()

   (* Private methods *)
   let callback responseMessage = Message.Backup(Response responseMessage) |> (this :> IActor<_>).Post

   let handleInitialStateMessage() = 
      fileChooser +! Message.FileChooser({ Payload = config; Callback = Some callback })
      Discovery

   let fireMessageToSolver files = spaceSolver +! Message.Solver({ Payload = { RootArchivePath = config.ArchiveFilePath; Files = files }; Callback = Some callback })

   let handleDiscoveryStateMessage msg =
      match msg with
      | ResponseMessage.FileChooser(files) -> 
         fireMessageToSolver files
         Solving({ AllFiles = files; ProcessedFiles = Seq.empty; RemainingFiles = files })
      | _ -> failwith "Unknown message"

   let handleSolvingStateMessage msg info =
      match msg with
      | ResponseMessage.Solver(targetFiles) ->
         archiver +! Message.Archiver({ Payload = { RootArchivePath = config.ArchiveFilePath; Files = targetFiles }; Callback = Some callback })
         Archiving(info)
      | _ -> failwith "Unknown message"

   let handleArchivingStateMessage msg info =
      match msg with
      | ResponseMessage.Archiver(archiverResponse) ->
         let processedFiles = Seq.cache <| seq { 
            let archivedFileEntries = archiverResponse.Archived |> Map.keys
            yield! info.ProcessedFiles
            yield! archivedFileEntries
            yield! archiverResponse.Failed 
         }

         (* -----> TODO: Not sure what to do about the failed files. <------ *)

         let storageReport = Map.remapKeys (fun (entry : FileEntry) -> entry.Path) archiverResponse.Archived
         manifestWriter +! Message.Manifest({ Payload = { RootArchivePath = config.ArchiveFilePath; StorageReport = storageReport }; Callback = Some callback })

         let remainingFiles = Seq.except info.RemainingFiles processedFiles |> Seq.cache
         WritingManifest({ info with ProcessedFiles = processedFiles; RemainingFiles = remainingFiles })
      | _ -> failwith "Unknown message"

   let handleWritingManifestStateMessage msg info =
      match msg with
      | ResponseMessage.Manifest(response) ->
         match response with
         | Success -> 
            if Seq.isEmpty info.RemainingFiles then
               Finished
            else
               volumeSwitcher +! Message.Switcher({ Payload = (); Callback = Some callback })
               Switching(info)
         | Failure -> Error
      | _ -> failwith "Unknown message"

   let handleSwitchingStateMessage msg info =
      match msg with
      | ResponseMessage.Switcher ->
         fireMessageToSolver info.RemainingFiles
         Solving(info)
      | _ -> failwith "Unknown message"

   let dispatch state request =
      let nextState = 
         match state, request with
         | Initial, Start -> handleInitialStateMessage()
         | Discovery, Response(msg) -> handleDiscoveryStateMessage msg
         | Solving(info), Response(msg) -> handleSolvingStateMessage msg info
         | Archiving(info), Response(msg) -> handleArchivingStateMessage msg info
         | WritingManifest(info), Response(msg) -> handleWritingManifestStateMessage msg info
         | Switching(info), Response(msg) -> handleSwitchingStateMessage msg info
         | Finished, _ -> Finished
         | Error, _ -> Error
         | _, _ -> failwith "Unknown state message combination"
      
      match nextState with
      | Finished -> parent +! BackupManagerResult.Finished
      | Error -> parent +! BackupManagerResult.Error
      | _ -> ()

      nextState

   (* Public methods *)
   override __.ProcessMessage state msg = 
      match msg with
      | Backup(request) -> dispatch state request
      | _ -> failwith "Unknown message"

   override __.ProcessShutdown state _ = 
      fileChooser +! Message.Shutdown
      spaceSolver +! Message.Shutdown
      archiver +! Message.Shutdown
      manifestWriter +! Message.Shutdown
      volumeSwitcher +! Message.Shutdown

      state