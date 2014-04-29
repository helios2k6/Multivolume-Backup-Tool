namespace MBT

open MBT.Core
open MBT.Operations
open SharpCompress.Common
open SharpCompress.Writer
open System
open System.IO

///<summary>A message that can be sent to an ArchiveActor</summary>
type ArchiveMessage = { ArchiveFile : String; Files : seq<String> }


///<summary>Archive result object</summary>
type ArchiveResult = 
   | UnableToOpenArchiveFile of String
   | UnableToOpenSourceFile of String
   | OutOfSpace
   | UnknownError
   | Success

///<summary>The response message from the ArchiveActor</summary>
type ArchiveResponseMessage = { Result : ArchiveResult; OriginalMessage : ArchiveMessage }

exception private ArchiveFileOpenException of String

exception private SourceFileOpenException of String

exception private WriterFactoryException

type ArchiveActor() =
   inherit ActorBase<ArchiveMessage, UnitPlaceHolder>()

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
         | _ -> raise WriterFactoryException

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
         | WriterFactoryException -> UnknownError
         | _ -> UnknownError

   override this.Receive sender msg state = 
      match msg with
      | { ArchiveMessage.ArchiveFile = archiveFile; ArchiveMessage.Files = files } -> 
         let archiveResult = this.ArchiveFiles archiveFile files
         sender +! { Message.Sender = this; Message.Payload = { ArchiveResponseMessage.Result = archiveResult; ArchiveResponseMessage.OriginalMessage = msg } }
      Hold

   override this.PreStart() = Hold
