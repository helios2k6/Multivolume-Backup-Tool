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

open MBT.Messages
open MBT.Operations
open System

///<summary>Holds the ApplicationConfiguration for the program</summary>
type ConfigurationManager(parent : IActor, inputActor : IActor, config : ApplicationConfiguration) =
   inherit ActorBase<ConfigurationMessage, ApplicationConfiguration>(parent)

   (* Private Methods *)
   member private this.ReconfigureListImpl oldConfig prompt =
      printfn prompt
      let pathToFolders = Console.ReadLine()
      pathToFolders.Split([|";"|], StringSplitOptions.RemoveEmptyEntries);

   member private this.ReconfigureArchiveFolders oldConfig = 
      let files = this.ReconfigureListImpl oldConfig "Type the paths to the folders you want to archive with each entry separated by a semicolon (;)"
      { oldConfig with Folders = files }

   member private this.ReconfigureBlacklist oldConfig =
      let files = this.ReconfigureListImpl oldConfig "Type the files you want to blacklist (Glob-syntax is acceptable)"
      { oldConfig with Blacklist = files }

   member private this.ReconfigureWhitelist oldConfig =
      let files = this.ReconfigureListImpl oldConfig "Type the files you want to whitelist (Glob-syntax is acceptable)"
      { oldConfig with Whitelist = files }

   member private this.ReconfigureArchiveFilePath oldConfig =
      printfn "Type the path that you would like to save the new archive file to\n"
      let pathOfNewArchiveFile = Console.ReadLine()
      { oldConfig with ApplicationConfiguration.ArchiveFilePath = pathOfNewArchiveFile }

   member private this.DispatchReconfiguration (promptOpt : String option) oldConfig f =
      match promptOpt with
      | Some(prompt) ->
         printfn "%A" prompt
         f oldConfig
      | None -> f oldConfig

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with
      | GetConfig -> 
         sender +! { Sender = this; Payload = Configuration(state) }
         state
      | SetConfig(newConfig) -> newConfig
      | ReconfigureArchiveFilePath(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureArchiveFilePath
         sender +! { Sender = this; Payload = Configuration(reconfiguredState) }
         reconfiguredState
      | ReconfigureFolders(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureArchiveFolders
         sender +! { Sender = this; Payload = Configuration(reconfiguredState) }
         reconfiguredState
      | ReconfigureBlacklist(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureBlacklist
         sender +! { Sender = this; Payload = Configuration(reconfiguredState) }
         reconfiguredState
      | ReconfigureWhitelist(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureWhitelist
         sender +! { Sender = this; Payload = Configuration(reconfiguredState) }
         reconfiguredState

   override this.PreStart() = config