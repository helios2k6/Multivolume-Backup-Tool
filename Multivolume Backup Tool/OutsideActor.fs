namespace MBT

open MBT.Operations

///<summary>An actor that can be used outside of the main actor system. It does not respond to any messages sent to it and can only send messages to targets</summary>
type OutsideActor() =
   interface IActor with
      member this.Post msg = ()
   end

   ///<summary>Send a message to a specified target</summary>
   member this.SendMessage msg (target : IActor) = target +! msg