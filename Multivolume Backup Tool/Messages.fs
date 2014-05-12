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
namespace MBT.Messages

open MBT
open Microsoft.FSharp.Collections
open System

(* The death message. It is sent to actors on shutdown *)
type DeathNote = Die

///<summary>The messages you can send to the File Chooser</summary>
type FileChooserMessage = ChooseFiles of ApplicationConfiguration

///<summary>The response message from the File Chooser</summary>
type FileChooserResponse = Files of seq<String>

///<summary>The messages you can send to the Knapsack Solver and the message you will get back as a response</summary>
type KnapsackMessage = Calculate of String * seq<String>

///<summary>The result of the knapsack solver</summary>
type KnapsackResponse = Files of seq<String>

///<summary>A message that can be sent to an Archiver</summary>
type ArchiveMessage = { ArchiveFilePath : String; Files : seq<String>; }

///<summary>The result of attempting to backup a particular file</summary>
type FileArchiveResult =
   | Success
   | FailureUnableToOpen
   | FailureOutOfSpace

///<summary>The response message from the Archiver</summary>
type ArchiveResponse = { BackedUpFiles : seq<String>; UnableToOpenFiles : seq<String>; FilesTooBig : seq<String> }

///<summary>The message you can send to the Backup Continuation Manager</summary>
type BackupContinuationMessage = { AllFiles : seq<String>; BackedUpFiles : seq<String>; ArchiveResponse : ArchiveResponse }

///<summary>The responses from the Backup Continuation Manager</summary>
type BackupContinuationResponse =
   | Finished
   | Abort
   | IgnoreFiles of seq<String>
   | ContinueProcessing

///<summary>The messages you can send the Backup Manager</summary>
type BackupMessage = Start

///<summary>The response message from the Backup Manager</summary>
type BackupResponse =
   | Success
   | Failure

///<summary>The message you can send to the Archive Resolver</summary>
type ArchiveResolverMessage = { ArchiveFilePath : String; Files : seq<String>; Client : IActor }

///<summary>The response you will get from the Archive Resolver</summary>
type ArchiveResolverResponse = { ArchiveFilePath : String; FileManifest : Map<String, String>; Files : seq<String>; Client : IActor } 