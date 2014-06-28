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

open MBT
open MBT.Core
open MBT.Core.Measure
open MBT.Core.Utilities
open MBT.Operations
open MBT.Messages
open System
open System.IO

///<summary>The actor that calculates the solution the knapsack problem</summary>
type KnapsackSolver(parent : IActor) =
   inherit ActorBase<KnapsackMessage, UnitPlaceHolder>(parent)

   (* Private Fields *)
   static let WiggleRoom = 50L<mebibyte> |> MebibytesToBytes

   (* Private Methods *)
   let DriveSpace rootPath = 
      let info = new DriveInfo(rootPath)
      info.AvailableFreeSpace |> WithByteMeasure 

   let SolveUsingGreedy files (availableCapacity : int64<byte>) = 
      let foldAction (state : Set<FileEntry> * int64<byte>) (item : FileEntry) =
         let selectedFiles = fst state
         let remainingCapacity = snd state
         if item.Size <= remainingCapacity then
            (selectedFiles.Add item, remainingCapacity - item.Size)
         else
            state

      List.fold foldAction (Set.empty, availableCapacity) files
      |> fst
      |> Seq.toList

   let PrintArchiveStats (totalToArchive : int64<byte>) (driveCapacity : int64<byte>) =
         PrintToConsole <| String.Format("Total amount to archive is: {0:n0} bytes", totalToArchive)
         PrintToConsole <| String.Format("Total destination drive capacity is: {0:n0} bytes", driveCapacity)
      
   let Solve archivePath (files : FileEntry list) = 
      let capacity = (Path.GetPathRoot archivePath |> DriveSpace) - WiggleRoom
      let totalAmountToArchive = List.sumBy (fun (item : FileEntry) -> item.Size) files

      PrintArchiveStats totalAmountToArchive capacity

      SolveUsingGreedy files capacity

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with
      | Calculate(archivePath, files) -> 
         PrintToConsole "Calculating files to store"
         sender +! Message.Compose this (KnapsackResponse.Files((Solve archivePath files)))
         Some Hold

   override this.PreStart() = Hold