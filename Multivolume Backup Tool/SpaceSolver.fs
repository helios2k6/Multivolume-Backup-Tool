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

open MBT.Core.Math
open MBT.Core.Measure
open MBT.Core.IO
open System.IO

/// <summary>
/// Determines which files will fit on the archive
/// </summary>
type internal SpaceSolver() = 
   inherit BaseStatelessActor()
   (* Private Fields *)
   static let WiggleRoom = 50L<mebibyte>
   
   (* Private methods *)
   let driveSpace path = (new DriveInfo(path)).AvailableFreeSpace |> WithByteMeasure

   let minusWiggleRoom capacity = max (capacity - (MebibytesToBytes WiggleRoom)) 0L<byte>

   let solveUsingGreedy files (capacity : int64<byte>) =
      let foldAction (state : Set<FileEntry> * int64<byte>) (item : FileEntry) =
         let selectedFiles = fst state
         let remainingCapacity = snd state
         if item.Size <= remainingCapacity then
            (selectedFiles.Add item, remainingCapacity - item.Size)
         else
            state

      Seq.fold foldAction (Set.empty, capacity) files |> fst

   override this.ProcessStatelessMessage msg =
      match msg with
      | Solver(actorMessage) ->
         match actorMessage.Callback with
         | Some(callback) -> 
            let archiveRootPath = actorMessage.Payload.RootArchivePath
            let files = actorMessage.Payload.Files
            let greedyResult = driveSpace archiveRootPath |> minusWiggleRoom |> solveUsingGreedy files
            ResponseMessage.Solver greedyResult |> callback
         | None -> failwith "Unable to callback"
      | _ -> failwith "Unknown message"