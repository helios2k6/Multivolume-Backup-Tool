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

open Knapsack
open Knapsack.Details
open MBT
open MBT.Core
open MBT.Core.Utilities
open MBT.Operations
open MBT.Messages
open System
open System.IO

///<summary>An IITem that represents a file</summary>
type FileItemWrapper(item : FileEntry, resolution : int64 -> int64) =
   (* Public Methods *)
   member this.File with get() = item

   interface IItem with
      member this.Value with get() = item.Info.Length |> resolution
      member this.Weight with get() = item.Info.Length |> resolution
   end

///<summary>The actor that calculates the solution the knapsack problem</summary>
type KnapsackSolver(parent : IActor) =
   inherit ActorBase<KnapsackMessage, UnitPlaceHolder>(parent)

   (* Private Fields *)
   static let WiggleRoom = 50L * mebibyte

   (* Private Methods *)
   let (|FileEntry|) (item : IItem) = (item :?> FileItemWrapper).File
   let (|AsIItem|) item = item :> IItem
   let (|FileSize|) (item : IItem) = item.Weight

   let SolveUsingGreedy archivePath (files : IItem list) (availableCapacity : int64) = 
      let foldAction (state : Set<FileEntry> * int64) (item : IItem) =
         let selectedFiles = fst state
         let remainingCapacity = snd state
         let size = item.Weight
         let fileEntry = (|FileEntry|) item
         if size <= remainingCapacity then
            (selectedFiles.Add fileEntry, remainingCapacity - size)
         else
            state

      List.fold foldAction (Set.empty, availableCapacity) files
      |> fst
      |> Seq.toList

   let SolveUsingDP archivePath (files : IItem list) (availableCapacity : int64) =
      let solver = new ZeroOneDPKnapsackSolver()

      solver.Solve(files, availableCapacity |> (|MebiBytes|))
      |> Seq.toList
      |> List.map (|FileEntry|)

   let Solve archivePath (files : FileEntry list) = 
      let filesAsIItems = 
         files 
         |> List.map (fun i -> new FileItemWrapper(i, (|MebiBytes|)) |> (|AsIItem|))
         |> List.sortBy (fun i -> i.Value)
         |> List.rev

      let totalAmountToArchive = List.fold (fun runningSize item -> runningSize + (|FileSize|) item) 0L filesAsIItems
      let rootPath = Path.GetPathRoot archivePath
      let capacity = (new DriveInfo(rootPath)).AvailableFreeSpace - WiggleRoom |> (|MebiBytes|)

      PrintToConsole <| sprintf "Total amount to archive is: %i mebibytes" totalAmountToArchive
      PrintToConsole <| sprintf "Total destination drive capaity is: %i mebibytes" ((|MebiBytes|) capacity)

      if totalAmountToArchive > (|MebiBytes|) (10L * gibibyte) then
         PrintToConsole "Total amount of files to archive is GREATER than 10 Gibibytes. Using greedy solution algorithm"
         SolveUsingGreedy archivePath filesAsIItems capacity
      else
         PrintToConsole "Total amount of files to archive is LESS than 10 Gibibytes. Using DP solution"
         SolveUsingDP archivePath filesAsIItems capacity

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with
      | Calculate(archivePath, files) -> 
         PrintToConsole "Calculating files to store"
         sender +! Message.Compose this (KnapsackResponse.Files((Solve archivePath files)))
         Some Hold

   override this.PreStart() = Hold