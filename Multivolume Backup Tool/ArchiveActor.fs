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
open MBT.Operations
open MBT.Messages
open SharpCompress.Common
open SharpCompress.Writer
open System
open System.IO

exception private ArchiveFileOpenException of String

exception private SourceFileOpenException of String

exception private WriterFactoryException of Exception

///<summary>An actor that archives files</summary>
type ArchiveActor(parent : IActor) =
   inherit ActorBase<ArchiveMessage, UnitPlaceHolder>(parent)

   (* Private Methods *)
   member private this.ArchiveFile (archiveFileWriter : IWriter) file = 
      let fileStream = File.OpenRead(file)
      archiveFileWriter.Write(file, fileStream)

   member private this.OpenArchiveFile archiveFile =
      try
         File.Open(archiveFile, FileMode.CreateNew, FileAccess.ReadWrite)
      with
         | _ -> raise (ArchiveFileOpenException(archiveFile))

   member private this.CreateArchiveWriter archiveFileStream =
      try
         WriterFactory.Open(archiveFileStream, ArchiveType.Tar, CompressionType.None)
      with
         | ex -> raise (WriterFactoryException(ex))

   member private this.OpenSourceFile sourceFile =
      try
         File.OpenRead(sourceFile)
      with
         | _ -> raise (SourceFileOpenException(sourceFile))

   member private this.ArchiveFiles archiveFile files = 
      try
         use archiveFileWriter = this.OpenArchiveFile archiveFile |> this.CreateArchiveWriter
         Seq.iter (fun file -> this.ArchiveFile archiveFileWriter file) files
         Success
      with
         | ArchiveFileOpenException(file) -> UnableToOpenArchiveFile(file)
         | SourceFileOpenException(file) -> UnableToOpenSourceFile(file)
         | WriterFactoryException(ex) -> UnknownError(ex)
         | ex -> UnknownError(ex)

   override this.Receive sender msg state = 
      match msg with
      | { ArchiveMessage.ArchiveFile = archiveFile; ArchiveMessage.Files = files } -> 
         let archiveResult = this.ArchiveFiles archiveFile files
         sender +! { Message.Sender = this; Message.Payload = { ArchiveResponseMessage.Result = archiveResult; ArchiveResponseMessage.OriginalMessage = msg } }
      Hold

   override this.PreStart() = Hold
