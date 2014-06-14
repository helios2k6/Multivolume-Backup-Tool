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

open MBT.Core
open MBT.Core.Utilities
open MBT.Messages
open MBT.Operations
open System

///<summary>Responses from the user</summary>
type private Response = 
   | Skip
   | Retry
   | Abort

type private SwitchVolume =
   | Yes
   | No

type private ErrorHandleStrategy =
   | NoError
   | Strategy of Response

///<summary>Manages the continuation from one backup session to the next by handling errors in the last backup</summary>
type BackupContinuationManager(parent : IActor) =
   inherit ActorBase<BackupContinuationMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)
   member private this.HandleErrorFiles files msg =
      PrintToConsole msg
      files |> List.iter (fun item -> printfn "\t%A" item)
      PrintToConsole "What would you like to do?"
      PrintToConsole "[S] Skip all files; [R] Retry all files; [A] Abort"

      let rec getResponseFromUser() =
         let response = Console.ReadKey()
         PrintNewLineToConsole()
         match response.Key with
         | ConsoleKey.A -> Abort
         | ConsoleKey.R -> Retry
         | ConsoleKey.S -> Skip
         | _ ->
            PrintToConsole <| sprintf "%A is an invalid choice. Please choose a valid choice" response.Key
            getResponseFromUser()
      
      getResponseFromUser()

   member private this.HandleUnableToOpenFiles files = 
      if List.isEmpty files then
         NoError
      else
         Strategy(this.HandleErrorFiles files "The following files could not be opened for copy:")

   member private this.HandleFilesTooBig files = 
      if List.isEmpty files then
         NoError
      else
         Strategy(this.HandleErrorFiles files "The following files were too large and could not be copied:")

   member private this.CalculateRemainingFiles allFiles processedFiles = (Set.ofSeq allFiles) - (Set.ofSeq processedFiles) |> Set.toList

   member private this.HandleNoErrorsState sender (msg : BackupContinuationMessage) = 
      let remainingFiles = this.CalculateRemainingFiles msg.AllFiles msg.BackedUpFiles
      if List.isEmpty remainingFiles then
         sender +! Message.Compose this Finished
      else
         sender +! Message.Compose this ContinueProcessing

   member private this.HandleSingleErrorState sender msg response = 
      match response with
      | Abort -> sender +! Message.Compose  this  BackupContinuationResponse.Abort
      | Skip -> 
         sender +! Message.Compose this (IgnoreFiles(msg.ArchiveResponse.UnableToOpenFiles @ msg.ArchiveResponse.FilesTooBig))
         sender +! Message.Compose this ContinueProcessing
      | Retry -> this.HandleNoErrorsState sender msg

   member private this.HandleDoubleErrorState sender msg unableToOpenResponse filesTooBigResponse = 
      match (unableToOpenResponse, filesTooBigResponse) with
      | (Abort, _) | (_, Abort) -> sender +! Message.Compose this BackupContinuationResponse.Abort
      | (Skip, Skip) -> this.HandleSingleErrorState sender msg unableToOpenResponse
      | (Skip, Retry) -> 
         sender +! Message.Compose this (IgnoreFiles(msg.ArchiveResponse.UnableToOpenFiles))
         this.HandleNoErrorsState sender msg
      | (Retry, Skip) -> 
         sender +! Message.Compose this (IgnoreFiles(msg.ArchiveResponse.FilesTooBig))
         this.HandleNoErrorsState sender msg
      | (Retry, Retry) -> this.HandleNoErrorsState sender msg

   (* Public Methods *)
   override this.Receive sender msg _ =
      let unableToOpenFilesStrat = this.HandleUnableToOpenFiles msg.ArchiveResponse.UnableToOpenFiles
      let filesTooBigStrat = this.HandleFilesTooBig msg.ArchiveResponse.FilesTooBig

      match (unableToOpenFilesStrat, filesTooBigStrat) with
      | (NoError, NoError) -> this.HandleNoErrorsState sender msg
      | (NoError, Strategy(response)) | (Strategy(response), NoError) -> this.HandleSingleErrorState sender msg response
      | (Strategy(response1), Strategy(response2)) -> this.HandleDoubleErrorState sender msg response1 response2

      Some Hold

   override this.PreStart() = Hold