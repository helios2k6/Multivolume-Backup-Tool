namespace MBT

open System
open MBT.Core
open Operations

///<summary>Reads input from the console and sends it back to the client</summary>
type InputActor =
   (* Private Methods *)
   member private this.ReadConsoleAsync prompt = 
      printfn "%A:" prompt
      Console.ReadLine()

   (* Public Methods *)
   inherit ActorBase<String, UnitPlaceHolder>
   override this.Receive sender msg state = 
      let userInput = this.ReadConsoleAsync msg
      sender +! { Sender = this; Payload = userInput }
      Hold

   override this.PreStart() = Hold