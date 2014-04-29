namespace MBT

open System
open Microsoft.FSharp.Control

[<AbstractClass>]
type ActorBase<'a> = 
   (* Fields *)
   member private this._mailbox = MailboxProcessor.Start this.InputLoop
   
   (* Private Methods *)
   member private this.InputLoop (inbox : MailboxProcessor<obj>) =
      let rec loop() =
         async {
            let! msg = inbox.Receive()

            match msg with
            | :? Messages.DeathNote -> return ()
            | :? 'a as aMsg -> this.Receive aMsg
            | _ -> ()

            return! loop()
         }
      loop()

   (* Public Methods *)

   ///<summary>Process a messge</summary>
   abstract member Receive : 'a -> unit

   ///<summary>Shutdown this actor</summary>
   member public this.Shutdown() = (this :> IActor).Post Messages.Die
      

   interface IActor with
      member this.Post msg = this._mailbox.Post msg
   end