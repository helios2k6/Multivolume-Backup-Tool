namespace MBT

open System
open MBT.Core
open MBT.Operations

type OutputActor() = 
   inherit ActorBase<String, UnitPlaceHolder>()
   (* Private Methods *)
   member private this.OutputToConsole prompt = printfn "%A" prompt

   (* Public methods *)
   override this.Receive sender msg state = 
      ignore(printfn "%A" msg)
      Hold

   override this.PreStart() = Hold