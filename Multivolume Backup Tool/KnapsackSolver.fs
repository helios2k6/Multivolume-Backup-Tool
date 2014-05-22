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
type FileItemWrapper(item : String) =
   let _fileInfo = new FileInfo(item)

   (* Public Methods *)
   member this.File with get() = item

   interface IItem with
      member this.Value with get() = _fileInfo.Length  |> (|MebiBytes|)
      member this.Weight with get() = _fileInfo.Length |> (|MebiBytes|)
   end

///<summary>The actor that calculates the solution the knapsack problem</summary>
type KnapsackSolver(parent : IActor) =
   inherit ActorBase<KnapsackMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)
   member private this.ExtractFileName (item : IItem) = (item :?> FileItemWrapper).File

   member private this.Solve archivePath (files : seq<String>) =
      let solver = new ZeroOneDPKnapsackSolver()
      let items = files |> Seq.map (fun item -> new FileItemWrapper(item) :> IItem)
      let rootDirectory = Path.GetPathRoot(archivePath)
      let driveInfo = new DriveInfo(rootDirectory)
      solver.Solve(items, driveInfo.AvailableFreeSpace |> (|MebiBytes|))
      |> Seq.map (fun i -> this.ExtractFileName i)

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with
      | Calculate(archivePath, files) -> 
         sender +! { Sender = this; Payload = KnapsackResponse.Files((this.Solve archivePath files)) }
         Hold

   override this.PreStart() = Hold