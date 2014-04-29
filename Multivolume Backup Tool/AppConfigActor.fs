namespace MBT

open MBT.Operations

///<summary>The messages you can send to an AppConfigActor</summary>
type AppConfigMessage = 
   ///<summary>Requests an ApplicationConfiguration record</summary>
   | GetConfig
   ///<summary>Requests that the the new ApplicationConfiguration be set</summary>
   | SetConfig of ApplicationConfiguration

///<summary>Holds the ApplicationConfiguration for the program</summary>
type AppConfigActor(config : ApplicationConfiguration) =
   inherit ActorBase<AppConfigMessage, ApplicationConfiguration>()

   override this.Receive sender msg state =
      match msg with
      | GetConfig -> 
         sender +! { Sender = this; Payload = state }
         state
      | SetConfig(newConfig) -> newConfig

   override this.PreStart() = config