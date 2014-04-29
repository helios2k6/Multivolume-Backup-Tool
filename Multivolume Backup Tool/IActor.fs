namespace MBT

///<summary>An actor object</summary>
type IActor = 
   ///<summary>Post a message to the actor</summary>
   abstract member Post : obj -> unit
