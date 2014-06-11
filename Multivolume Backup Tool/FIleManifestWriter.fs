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

 open Newtonsoft.Json
 open MBT.Core
 open MBT.Core.Utilities
 open MBT.Operations
 open MBT.Messages
 open System
 open System.IO

 ///<summary>Writes the file manifest to the archive directory</summary>
 type FileManifestWriter(parent : IActor) =
   inherit ActorBase<FileManifestWriterMessage, UnitPlaceHolder>(parent)
   
   let TryWriteManifestFile archivePath fileManifest =
      try
         let serializedContext = JsonConvert.SerializeObject(fileManifest, Formatting.Indented)
         let manifestFilePath = Path.Combine(archivePath, Constants.FileManifestFileName)
         PrintToConsole <| sprintf "Writing manifest file to: %s" manifestFilePath
         File.WriteAllText(manifestFilePath, serializedContext)
         FileManifestWriterResponse.Success
      with
         | _ -> FileManifestWriterResponse.Failure

   override this.Receive sender msg _ =
      match msg with
      | WriteManifest(archivePath, fileManifest) -> 
         let result = TryWriteManifestFile archivePath fileManifest
         sender +! Message.Compose this result
      Hold

   override this.PreStart() = Hold