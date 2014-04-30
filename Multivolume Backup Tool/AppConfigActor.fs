namespace MBT

open MBT.Operations
open System

///<summary>The messages you can send to an AppConfigActor</summary>
type AppConfigMessage = 
   ///<summary>Requests an ApplicationConfiguration record</summary>
   | GetConfig
   ///<summary>Requests that the the new ApplicationConfiguration be set</summary>
   | SetConfig of ApplicationConfiguration
   ///<summary>Reconfigure the archive file path</summary>
   | ReconfigureArchiveFilePath of String option
   ///<summary>Reconfigure the folders to archive</summary>
   | ReconfigureFolders of String option
   ///<summary>Reconfigure the blacklist</summary>
   | ReconfigureBlacklist of String option
   ///<summary>Reconfigure the whitelist</summary>
   | ReconfigureWhitelist of String option

///<summary>Holds the ApplicationConfiguration for the program</summary>
type AppConfigActor(parent : IActor, inputActor : IActor, config : ApplicationConfiguration) =
   inherit ActorBase<AppConfigMessage, ApplicationConfiguration>(parent)

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
      { oldConfig with ArchiveFilePath = pathOfNewArchiveFile }

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
         sender +! { Sender = this; Payload = state }
         state
      | SetConfig(newConfig) -> newConfig
      | ReconfigureArchiveFilePath(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureArchiveFilePath
         sender +! { Sender = this; Payload = reconfiguredState }
         reconfiguredState
      | ReconfigureFolders(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureArchiveFolders
         sender +! { Sender = this; Payload = reconfiguredState }
         reconfiguredState
      | ReconfigureBlacklist(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureBlacklist
         sender +! { Sender = this; Payload = reconfiguredState }
         reconfiguredState
      | ReconfigureWhitelist(promptOpt) -> 
         let reconfiguredState = this.DispatchReconfiguration promptOpt state this.ReconfigureWhitelist
         sender +! { Sender = this; Payload = reconfiguredState }
         reconfiguredState

   override this.PreStart() = config