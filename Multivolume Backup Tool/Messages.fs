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

open MBT.Core.IO

/// <summary>
/// Represents the response message from the archiver
/// </summary>
type internal ArchiverResponse = { Archived : Map<FileEntry, string>; Failed : FileEntry seq }

/// <summary>
/// The standard response to any request
/// </summary>
type internal StandardResponse = Success | Failure

/// <summary>
/// Response messages that can be sent back as a callback parameter
/// </summary>
type internal ResponseMessage =
   | ManifestProcessor of Map<string, string> option
   | Solver of FileEntry seq
   | FileChooser of FileEntry seq
   | Archiver of ArchiverResponse
   | Manifest of StandardResponse
   | Continuation of FileEntry seq
   | Switcher

/// <summary>
/// An alias over the ActorMessageAbstract<a, b> generic. This is just to 
/// reduce typing
/// </summary>
type internal ActorMessage<'a> = ActorMessageAbstract<'a, ResponseMessage>
/// <summary>
/// An alias over the ActorMessage<unit> type. This is meant used for messages
/// that serve no purpose other than signal to another actor
/// </summary>
type internal ActorSignal = ActorMessage<unit>

/// <summary>
/// A standard request record that has the root archive path and a file entry sequence
/// </summary>
type internal StandardRequest = { RootArchivePath : string; Files : FileEntry seq }

/// <summary>
/// A request record to the manifest writer
/// </summary>
type internal ManifestRequest = { RootArchivePath : string; Manifest : Map<FileEntry, string> }

/// <summary>
/// A request record to the continuation manager
/// </summary>
type internal ContinuationRequest = { Remaining : FileEntry seq; LatestArchive : FileEntry seq; }

/// <summary>
/// The types of messages that can be sent to the actors of this backup system
/// </summary>
type internal Message =
   | Console of string
   | FileChooser of ActorMessage<ApplicationConfiguration>
   | ManifestProcessor of ActorMessage<string>
   | Solver of ActorMessage<StandardRequest>
   | Archiver of ActorMessage<StandardRequest>
   | Manifest of ActorMessage<ManifestRequest>
   | Continuation of ActorMessage<ContinuationRequest>
   | Switcher of ActorSignal
   | Backup
   | Shutdown