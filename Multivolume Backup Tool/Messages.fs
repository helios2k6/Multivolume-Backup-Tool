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

open MBT.Core.IO

/// <summary>
/// Represents the response message from the archiver
/// </summary>
type internal ArchiverResponse = { Archived : FileEntry seq; Failed : FileEntry seq }

/// <summary>
/// Response messages that can be sent back as a callback parameter
/// </summary>
type internal ResponseMessage =
   | ManifestProcessor of Map<string, string> option
   | Solver of FileEntry seq
   | FileChooser of FileEntry seq
   | Archiver of ArchiverResponse

/// <summary>
/// An alias over the ActorMessageAbstract<a, b> generic. This is just to 
/// reduce typing
/// </summary>
type internal ActorMessage<'a> = ActorMessageAbstract<'a, ResponseMessage>

///<summary>
/// A standard request record that has the root archive path and a file entry sequence
///</summary>
type internal StandardRequest = { RootArchivePath : string; Files : FileEntry seq }

/// <summary>
/// The types of messages that can be sent to the actors of this backup system
/// </summary>
type internal Message =
   | Console of string
   | FileChooser of ActorMessage<ApplicationConfiguration>
   | ManifestProcessor of ActorMessage<string>
   | Solver of ActorMessage<StandardRequest>
   | Archiver of ActorMessage<StandardRequest>
   | Backup
   | Shutdown
