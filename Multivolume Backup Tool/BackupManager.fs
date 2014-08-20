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
type internal BackupManager(config : ApplicationConfiguration) =
   inherit BaseStateActor<BackupManagerState>(Initial)

   (* Private fields *)
   let fileChooser = new FileChooser()
   let spaceSolver = new SpaceSolver()
   let manifestProcessor = new ManifestProcessor()
   let continuationProcessor = new ContinuationProcessor()
   let volumeSwitcher = new VolumeSwitcher()

   (* Private methods *)
   let handleSwitchingStateMessage msg =
      match msg with
      | ResponseMessage.Switcher -> Error
      | _ -> failwith "Unknown message"

   let handleContinuationStateMessage msg =
      match msg with
      | ResponseMessage.Continuation(files) -> Error
      | _ -> failwith "Unknown message"

   let handleWritingManifestStateMessage msg =
      match msg with
      | ResponseMessage.Manifest(result) -> Error
      | _ -> failwith "Unknown message"

   let handleArchivingStateMessage msg =
      match msg with
      | ResponseMessage.Archiver(response) -> Error
      | _ -> failwith "Unknown message"

   let handleSolvingStateMessage msg =
      match msg with
      | ResponseMessage.Solver(files) -> Error
      | _ -> failwith "Unknown message"

   let handlePreprocessingStateMessage msg =
      match msg with
      | ResponseMessage.ManifestProcessor(oldManifest) -> Error
      | _ -> failwith "Unknown message"

   let handleDiscoveryStateMessage msg =
      match msg with
      | ResponseMessage.FileChooser(files) -> Error
      | _ -> failwith "Unknown message"

   let handleInitialStateMessage msg = 
      match msg with
      | Backup -> Error
      | _ -> failwith "Unknown message"

   (* Public methods *)
   override this.ProcessMessage state msg = 
      match state with
      | Initial -> handleInitialStateMessage msg
      | Finished -> failwith "Backup already finished. Cannot accept any more messages"
      | Error -> failwith "Backup has error'ed out. Cannot accept any more messages"
      | _ -> failwith "Backup unable to accept messages. Invalid state"

   override this.PreShutdown state msg = failwith "Not implemented yet"

   override this.PostShutdown state msg = failwith "Not implemented yet"