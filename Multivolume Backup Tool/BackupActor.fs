namespace MBT

open System
open Microsoft.FSharp.Collections

///<summary>The messages you can send the backup actor</summary>
type BackupMessage = 
   | Start
   | SetArchiveActor of IActor


type BackupActorState = { FilesBackedUp : Set<String>; BackupActor : IActor option }

type BackupActor() =
   inherit ActorBase<BackupMessage, BackupActorState>()

   (* Private Methods *)
   member private this.BackupFiles state = state

   (* Public Methods *)
   override this.Receive sender msg state =
      match msg with 
      | SetArchiveActor(actor) -> { state with BackupActor = Some(actor) }
      | Start -> this.BackupFiles state

   override this.PreStart() = { FilesBackedUp = Set.empty; BackupActor = None }