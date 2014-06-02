(*
 * The MIT License (MIT)
 *
 * Copyright (c) 2014 Andrew B. Johnson
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *)

namespace MBT

open log4net
open MBT
open MBT.Core
open MBT.Core.Utilities
open MBT.Messages
open MBT.Operations
open System

///<summary>Handles any physical volume switching that the user has to do</summary>
type VolumeSwitcher(parent : IActor) =
   inherit ActorBase<VolumeSwitcherMessage, UnitPlaceHolder>(parent)

   (* Private Static Fields *)
   static let Log = LogManager.GetLogger typedefof<VolumeSwitcher>

   (* Private Methods *)
   member this.PromptUserToSwitchVolumes currentVolumePath =
      let rec getResponseFromUser() =
         let response = Console.ReadKey()
         PrintNewLineToConsole()
         match response.Key with
         | ConsoleKey.Y -> true
         | ConsoleKey.N -> false
         | _ ->
            PrintToConsole <| sprintf "%A is not a valid choice. Please choose a valid choice" response.Key
            getResponseFromUser()

      PrintToConsole "Prepare the next volume. Hit return when ready..."
      Console.ReadLine() |> ignore
      PrintToConsole <| sprintf "The current archive path is: %A" currentVolumePath
      PrintToConsole "Would you like to change the archive path?"
      PrintToConsole "[Y] Yes. [N] No."
      if getResponseFromUser() then
         PrintToConsole "Where would you like to save your files?"
         Console.ReadLine()
      else
         PrintToConsole "OK, using the same volume path to save files"
         currentVolumePath         

   override this.Receive sender msg state = 
      match msg with 
         | SwitchVolumes(currentVolumePath) ->  
            let newVolumePath = this.PromptUserToSwitchVolumes currentVolumePath
            sender +! Message.Compose this (VolumePath(newVolumePath))
      Hold

   override this.PreStart() = Hold