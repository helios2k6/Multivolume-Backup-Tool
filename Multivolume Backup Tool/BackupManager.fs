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

/// <summary>
/// The possible states the Backup Manager can be in
/// </summary>
type internal BackupManagerState = 
   | Initial
   | Discovery
   | Preprocessing
   | Solving
   | Archiving
   | WritingManifest
   | Continuation
   | Switching
   | Finished
   | Error

/// <summary>
/// The main actor for managing backup operations
/// </summary>
type internal BackupManager(config : ApplicationConfiguration) as this =
   inherit BaseStateActor<BackupManagerState>(Initial)

   (* Private fields *)
   let fileChooser = new FileChooser()
   let manifestProcessor = new ManifestProcessor()
   let spaceSolver = new SpaceSolver()
   let continuationProcessor = new ContinuationProcessor()
   let volumeSwitcher = new VolumeSwitcher()

   (* Private methods *)
   let callback responseMessage = Message.Backup(Response responseMessage) |> (this :> IActor<_>).Post

   let handleInitialStateMessage() = 
         fileChooser +! Message.FileChooser({ Payload = config; Callback = Some callback })
         Discovery

   let handleDiscoveryStateMessage msg =
      match msg with
      | ResponseMessage.FileChooser(files) ->
         manifestProcessor +! Message.ManifestProcessor({ Payload = config.ArchiveFilePath; Callback = Some callback })
         Preprocessing
      | _ -> failwith "Unknown message"

   let dispatch state request =
      match state, request with
      | Initial, Start -> handleInitialStateMessage()
      | Discovery, Response(msg) -> handleDiscoveryStateMessage msg
      | Preprocessing, Response(msg)  -> Error
      | Solving, Response(msg)  -> Error
      | Archiving, Response(msg)  -> Error
      | WritingManifest, Response(msg)  -> Error
      | Continuation, Response(msg)  -> Error
      | Switching, Response(msg)  -> Error
      | Finished, _ -> Error
      | Error, _ -> Error
      | _, _ -> failwith "Unknown state message combination"

   (* Public methods *)
   override this.ProcessMessage state msg = 
      match msg with
      | Backup(request) -> dispatch state request
      | _ -> failwith "Unknown message"

   override this.PreShutdown state msg = failwith "Not implemented yet"

   override this.PostShutdown state msg = failwith "Not implemented yet"