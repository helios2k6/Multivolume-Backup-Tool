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

open MBT.Console
open MBT.Core
open MBT.Core.IO
open System.IO
open System.Text.RegularExpressions

/// <summary>
/// Actor that discovers which files to archive
/// </summary>
type internal FileChooser() =
   inherit BaseStatelessActor()
   
   (* Private methods *)
   let stringLike str wildcard = 
      let regex = new Regex("^" + Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase ||| RegexOptions.Singleline)
      regex.IsMatch(str)

   let isFileOnList file fileList = fileList |> Seq.exists (fun pattern -> stringLike file pattern)

   let shouldAcceptFile file blacklist whitelist =
      if not <| File.Exists file then
         false
      elif isFileOnList file blacklist then
         false
      elif isFileOnList file whitelist then
         true
      elif not <| Seq.isEmpty whitelist then
         false
      else
         true

   let computeFileEntryAsync (entry : FileEntry) = async {
      entry.Compute()
      return entry
   }

   let chooseFiles (config : ApplicationConfiguration) =
      try
         query {
            for folder in config.Folders do
            for file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories) do
            where (shouldAcceptFile file config.Blacklist config.Whitelist)
            select (new FileEntry(file))
         }
         |> Seq.map computeFileEntryAsync
         |> Async.Parallel
         |> Async.RunSynchronously :> FileEntry seq
      with 
      | _ -> 
         puts <| sprintf "Unable to open folders %A" config.Folders
         Seq.empty

   (* Public methods *)
   override __.ProcessStatelessMessage msg =
      match msg with
      | FileChooser(actorMessage) -> 
         match actorMessage.Callback with
         | Some(callback) -> 
            actorMessage.Payload
            |> chooseFiles
            |> ResponseMessage.FileChooser
            |> callback
         | _ -> failwith "Unable to callback"
      | _ -> failwith "Unknown message"