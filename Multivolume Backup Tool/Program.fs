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

open MBT
open MBT.Core
open MBT.Console
open CommandLine
open CommandLine.Text
open Microsoft.FSharp.Control
open System.IO

type private WaitAgentState = Waiting | Done

type private Message = Callback | Wait of AsyncReplyChannel<unit>

let private AgentLoop (inbox : MailboxProcessor<Message>) =
   let rec loop (state : WaitAgentState) =
      async {
         let! msg = inbox.Receive()

         match msg with
         | Callback -> return! loop Done
         | Wait (replyChannel) ->
            match state with
            | Waiting -> 
               let rec innerLoop() =
                  async {
                     let! nextMsg = inbox.Receive()
                     match nextMsg with
                     | Callback -> replyChannel.Reply()
                     | _ -> return! innerLoop()
                  }
               do! innerLoop()
            | Done -> replyChannel.Reply()
      }
   loop Waiting

[<EntryPoint>]
let main argv = 
   puts "Starting Multivolume Backup Tool"
   let parsedArgs = ArgumentParser.ParseArguments argv

   if parsedArgs.State <> null && parsedArgs.State.Errors.Count > 0 then
      ArgumentParser.PrintHelp parsedArgs
   else
      puts "Beginning backup process"
      let appConfig = ApplicationConfigurationFactory.CreateConfiguration parsedArgs
      puts "Finished backup process"
      
   0 // return an integer exit code