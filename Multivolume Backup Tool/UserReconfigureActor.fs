namespace MBT

open MBT.Core
open System

///<summary>Messages that you can send to the User Reconfigure Actor</summary>
type UserReconfigureMessage = 
   ///<summary>Reconfigure the archive file path</summary>
   | ReconfigureArchiveFilePath of String option
   ///<summary>Reconfigure the folders to archive</summary>
   | ReconfigureFolders of String option
   ///<summary>Reconfigure the blacklist</summary>
   | ReconfigureBlacklist of String option
   ///<summary>Reconfigure the whitelist</summary>
   | ReconfigureWhitelist of String option

///<summary>Response messages sent by the User Reconfigure Actor</summary>
type UserReconfigureResponse =
   | Success
   | FailureUserAbort

///<summary>
///The User Reconfiguration Actor. It's main job is to reconfigure the application's configuration record
///</summary>
type UserReconfigureActor() =
   inherit ActorBase<UserReconfigureMessage, UnitPlaceHolder>()

   (* Private Methods *)
   member private this.ReconfigureArchiveFilePathImpl() = ()

   member private this.ReconfigureArchiveFilePath promptOption = 
      match promptOption with
      | Some(msg) -> 
         printfn msg
         this.ReconfigureArchiveFilePathImpl()
      | None -> this.ReconfigureArchiveFilePathImpl()

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with
      | ReconfigureArchiveFilePath(promptOpt) -> state
      | ReconfigureFolders(promptOpt) -> state
      | ReconfigureBlacklist(promptOpt) -> state
      | ReconfigureWhitelist(promptOpt) -> state

   override this.PreStart() = Hold