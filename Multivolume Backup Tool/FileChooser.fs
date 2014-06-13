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
open MBT.Core.Utilities
open MBT.Messages
open MBT.Operations
open System
open System.IO
open System.Text.RegularExpressions

///<summary>Chooses which files to archive</summary>
type FileChooser(parent : IActor) =
   inherit ActorBase<FileChooserMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)
   member private this.StringLike str wildcard =
      let regex = new Regex("^" + Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase ||| RegexOptions.Singleline)
      regex.IsMatch(str)

   member private this.IsFileOnList (file : String) (files : seq<String>) = files |> Seq.exists (fun pattern -> this.StringLike file pattern)

   member private this.ShouldAcceptFile (file : String) (blacklist : seq<String>) (whitelist : seq<String>) =
      if this.IsFileOnList file blacklist then
         false
      elif this.IsFileOnList file whitelist then
         true
      elif not (Seq.isEmpty whitelist) then
         false
      else
         true

   member private this.TryChooseFiles (config : ApplicationConfiguration) =
      try
         query {
            for folder in config.Folders do
            for file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories) do
            where (this.ShouldAcceptFile file config.Blacklist config.Whitelist)
            select file
         }
         |> Seq.toList
         |> Some
      with 
      | ex -> 
         PrintToConsole <| sprintf "Unable to open folders %A" config.Folders
         None

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with
      | ChooseFiles(config) ->
         PrintToConsole "Choosing files to backup"
         let chosenFilesOption = this.TryChooseFiles config 
         match chosenFilesOption with
         | Some(folders) -> sender +! Message.Compose this (FileChooserResponse.Files(folders))
         | None -> sender +! Message.Compose this FileChooserResponse.Failure
         Some Hold

   override this.PreStart() = Hold