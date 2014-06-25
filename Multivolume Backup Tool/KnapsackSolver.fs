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
open MBT.Core.Measure
open MBT.Core.Utilities
open MBT.Operations
open MBT.Messages
open System
open System.IO

///<summary>An IITem that represents a file</summary>
type FileItemWrapper(item : FileEntry, resolution : int64 -> int64) =
   (* Public Methods *)
   member this.File with get() = item

   member this.Size with get() = item.Size

   interface IItem with
      member this.Value with get() = item.Info.Length |> resolution
      member this.Weight with get() = (this :> IItem).Value
   end

type private Resolution = Bytes | Kibibytes | Mebibytes | Gibibytes

///<summary>The actor that calculates the solution the knapsack problem</summary>
type KnapsackSolver(parent : IActor) =
   inherit ActorBase<KnapsackMessage, UnitPlaceHolder>(parent)

   (* Private Fields *)
   static let WiggleRoom = 50L<mebibyte>
   static let DPDriveCapacityCeiling = 10L<gibibyte>

   (* Private Methods *)
   let FileEntry (item : IItem) = (item :?> FileItemWrapper).File
   
   let AsIItem item = item :> IItem
   
   let DetermineResolution (capacity : int64<byte>) = 
      let resolutionTransform magnitudeTransform length = length |> WithByteMeasure |> magnitudeTransform |> WithoutMeasure

      let expandedCapacity = capacity + (capacity / 10L) //Extra wiggle room

      if expandedCapacity >= 1L<tebibyte> * bytesPerTebibyte then
         resolutionTransform BytesToGibibytes, Gibibytes
      elif expandedCapacity >= 1L<gibibyte> * bytesPerGibibyte then
         resolutionTransform BytesToMebibytes, Mebibytes
      elif expandedCapacity >= 1L<mebibyte> * bytesPerMebibyte then
         resolutionTransform BytesToKibibytes, Kibibytes
      else
         (fun length -> length), Bytes

   let DriveSpace rootPath = 
      let info = new DriveInfo(rootPath)
      info.AvailableFreeSpace |> WithByteMeasure |> BytesToMebibytes

   let SolveUsingGreedy archivePath files (availableCapacity : int64<mebibyte>) = 
      let foldAction (state : Set<FileEntry> * int64<mebibyte>) (item : IItem) =
         let selectedFiles = fst state
         let remainingCapacity = snd state
         let size = item.Weight * 1L<mebibyte>
         let fileEntry = FileEntry item
         if size <= remainingCapacity then
            (selectedFiles.Add fileEntry, remainingCapacity - size)
         else
            state

      List.fold foldAction (Set.empty, availableCapacity) files
      |> fst
      |> Seq.toList

   let SolveUsingDP archivePath files (availableCapacity : int64<mebibyte>) =
      let solver = new ZeroOneDPKnapsackSolver()

      solver.Solve(files, availableCapacity |> WithoutMeasure) 
      |> Seq.toList 
      |> List.map FileEntry
      
      
   let Solve archivePath (files : FileEntry list) = 
      let printArchiveStats totalToArchive driveCapacity resolution =
         let resolutionString = match resolution with
                                | Bytes -> "Bytes"
                                | Kibibytes -> "Kibibytes"
                                | Mebibytes -> "Mebibytes"
                                | Gibibytes -> "Gibibytes"

         PrintToConsole <| sprintf "Total amount to archive is: %i %s" (WithoutMeasure totalToArchive) resolutionString
         PrintToConsole <| sprintf "Total destination drive capacity is: %i %s" (WithoutMeasure driveCapacity) resolutionString

      let capacity = (Path.GetPathRoot archivePath |> DriveSpace) - WiggleRoom

      let resolutionTransformation, resolution = MebibytesToKibibytes >> KibibytesToBytes >> DetermineResolution <| capacity

      let filesAsIItems = 
         files 
         |> List.map (fun i -> new FileItemWrapper(i, resolutionTransformation) |> AsIItem)
         |> List.sortBy (fun i -> i.Value)
         |> List.rev

      let totalAmountToArchive = List.sumBy (fun (item : IItem) -> item.Weight) filesAsIItems

      printArchiveStats totalAmountToArchive capacity resolution

      if totalAmountToArchive > DPDriveCapacityCeiling then
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