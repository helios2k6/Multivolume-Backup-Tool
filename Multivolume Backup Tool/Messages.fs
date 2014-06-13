﻿(*
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

///<summary>The shutdown message sent to actors</summary>
type ShutdownMessage = Start

///<summary>The response sent by an actor that was told to shutdown</summary>
type ShutdownResponse = FinishedShutdown | FailedShutdown

///<summary>The messages you can send to the File Chooser</summary>
type FileChooserMessage = ChooseFiles of ApplicationConfiguration

///<summary>The response message from the File Chooser</summary>
type FileChooserResponse = Files of seq<String> | Failure

///<summary>The message you can send to the Archive Resolver</summary>
type ArchiveResolverMessage = { ArchiveFilePath : String; Files : String seq }

///<summary>The response you will get from the Archive Resolver</summary>
type ArchiveResolverResponse = { ArchiveFilePath : String; FileManifest : Map<String, String>; Files : String seq } 

///<summary>The messages you can send to the Knapsack Solver and the message you will get back as a response</summary>
type KnapsackMessage = Calculate of String * String seq

///<summary>The result of the knapsack solver</summary>
type KnapsackResponse = Files of String seq

///<summary>A message that can be sent to an Archiver</summary>
type ArchiveMessage = { ArchiveFilePath : String; Files : String seq; }

///<summary>The result of attempting to backup a particular file</summary>
type FileArchiveResult =
   | Success
   | FailureUnableToOpen
   | FailureOutOfSpace

///<summary>The response message from the Archiver</summary>
type ArchiveResponse = { BackedUpFiles : (String * String) seq; UnableToOpenFiles : String seq; FilesTooBig : String seq }

///<summary>A message that can be sent to the File Manifest Writer</summary>
type FileManifestWriterMessage = WriteManifest of String * Map<String, String>

///<summary>The response message from the File Manifest Writer</summary>
type FileManifestWriterResponse = Success | Failure

///<summary>The message you can send to the Backup Continuation Manager</summary>
type BackupContinuationMessage = { AllFiles : String seq; BackedUpFiles : String seq; ArchiveResponse : ArchiveResponse }

///<summary>The responses from the Backup Continuation Manager</summary>
type BackupContinuationResponse =
   | Finished
   | Abort
   | IgnoreFiles of String seq
   | ContinueProcessing

///<summary>The message you can send to the Volume Switcher</summary>
type VolumeSwitcherMessage = SwitchVolumes of String

///<summary>The response you will get from the Volume Switcher</summary
type VolumeSwitcherResponse = VolumePath of String

///<summary>The messages you can send the Backup Manager</summary>
type BackupMessage = Start

///<summary>The response message from the Backup Manager</summary>
type BackupResponse = Success | Failure