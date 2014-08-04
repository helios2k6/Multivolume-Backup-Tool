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
/// Response messages that can be sent back as a callback parameter
/// </summary>
type internal ResponseMessage =
   | ManifestProcessorResponse of Map<string, string> option
   | SolverResponse of seq<FileEntry>

/// <summary>
/// An alias over the ActorMessageAbstract<a, b> generic. This is just to 
/// reduce typing
/// </summary>
type internal ActorMessage<'a> = ActorMessageAbstract<'a, ResponseMessage>

/// <summary>
/// The types of messages that can be sent to the actors of this backup system
/// </summary>
type internal Message =
   | Shutdown
   | BackupMessage
   | SolverMessage of ActorMessage<string * seq<FileEntry>>
   | ManifestProcessorMessage of ActorMessage<string>
   | ConsoleMessage of string

