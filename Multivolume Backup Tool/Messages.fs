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

///<summary>The messages you can send the backup actor</summary>
type BackupMessage = 
   | Start
   | SetArchiveActor of IActor
   | SetKnapsackActor of IActor
   | SetBackupErrorActor of IActor

///<summary>A message that can be sent to an ArchiveActor</summary>
type ArchiveMessage = { ArchiveFile : String; Files : seq<String> }

///<summary>The messages you can send to an AppConfigActor</summary>
type AppConfigMessage = 
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

///<summary>The messages you can send to the Knapsack Actor</summary>
type KnapsackMessage = Calculate of String * seq<String>

type FileChooserMessage = ChooseFiles of ApplicationConfiguration