namespace MBT

open System
open Microsoft.FSharp.Collections

///<summary>The messages you can send the backup actor</summary>
type BackupMessage = Start

type BackupActor() =
   inherit ActorBase<BackupMessage, Set<String>>()

   (* Public Methods *)
   override this.Receive sender msg state =
      state

   override this.PreStart() = Set.empty