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
open System

(* The death message. It is sent to actors on shutdown *)
type DeathNote = Die

///<summary>The messages you can send to an Configuration Manager</summary>
type ConfigurationMessage = 
   ///<summary>Requests an ApplicationConfiguration record</summary>
   | GetConfig
   ///<summary>Requests that the the new ApplicationConfiguration be set</summary>
   | SetConfig of ApplicationConfiguration
   ///<summary>Reconfigure the archive file path</summary>
   | ReconfigureArchiveFilePath of String option
   ///<summary>Reconfigure the folders to archive</summary>
   | ReconfigureFolders of String option
   ///<summary>Reconfigure the blacklist</summary>
   | ReconfigureBlacklist of String option
   ///<summary>Reconfigure the whitelist</summary>
   | ReconfigureWhitelist of String option

///<summary>The response from the Configuration Manager</summary>
type ConfigurationResponse = Configuration of ApplicationConfiguration

///<summary>The messages you can send to the Knapsack Solver</summary>
type KnapsackMessage = Calculate of String * seq<String>

///<summary>The response message sent from the Knapsack Solver</summary>
type KnapsackResponse = Files of seq<String>

///<summary>The messages you can send to the File Chooser</summary>
type FileChooserMessage = ChooseFiles of ApplicationConfiguration

///<summary>The response message from the File Chooser</summary>
type FileChooserResponse = Files of seq<String>

///<summary>A message that can be sent to an Archiver</summary>
type ArchiveMessage = { ArchiveFilePath : String; Files : seq<String> }

///<summary>Archive result object</summary>
type ArchiveResult = 
   | UnableToOpenArchiveFilePath of String
   | UnableToOpenSourceFile of String
   | UnknownError of Exception
   | Success

///<summary>The response message from the Archiver</summary>
type ArchiveResponse = { Result : ArchiveResult; OriginalMessage : ArchiveMessage }

///<summary>The messages you can send the Backup Manager</summary>
type BackupMessage = 
   | Start 
   | SetArchiveActor of IActor
   | SetKnapsackActor of IActor
   | SetBackupErrorActor of IActor
   | SetFileChooser of IActor
   | SetApplicationConfiguration of ApplicationConfiguration

///<summary>The response message from the Backup Manager</summary>
type BackupResponse =
   | Success
   | FailureArchiverNotSet
   | FailureKnapsackSolverNotSet
   | FailureFileChooserNotSet
   | FailureArchiveFilePathNotSet
   | FailureBackupErrorHandlerNotSet
   | FailureAbort of Exception option

///<summary>The messages you can send to the Backup Error Handler</summary>
type BackupErrorHandlerMessage =
   ///<summary>Cannot open the archive file path message. Pass the file path as a string</summary>
   | CannotOpenArchiveFilePath of String
   ///<summary>Cannot copy a file to the archive file path due to an IO error (except out of disk space errors)</summary>
   | CannotCopyFileToArchivePath of String
   ///<summary>Out of space errors</summary>
   | OutOfSpace of String

///<summary>The responses from the Backup Error Handler</summary>
type BackupErrorHandlerResponse =
   ///<summary>Abort the program</summary>
   | Abort
   ///<summary>Retry the file or folder path</summary>
   | Retry
   ///<summary>Skip the file</summary>
   | Skip
   ///<summary>Replace the volume to which the archive file path points to</summary>
   | ReplaceVolume
   ///<summary>Ask user for a new file path</summary>
   | AskForNewArchiveFilePath