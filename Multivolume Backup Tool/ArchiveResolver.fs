namespace MBT

open MBT.Core
open MBT.Operations
open MBT.Messages

type ArchiveResolver(parent : IActor) =
   inherit ActorBase<ArchiveResolverMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)

   (* Public Methods *)
   override this.Receive sender msg state =
      Hold

   override this.PreStart() = Hold