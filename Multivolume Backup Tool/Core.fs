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
namespace MBT.Core

open System

///<summary>A placeholder type for the "Unit" type for generic parameters</summary>
type UnitPlaceHolder = Hold

///<summary>Utility module</summary>
module Utilities =
   let private BytesOr1 bytes = if bytes = 0L then 1L else bytes

   ///<summary>Represents 1 Kibibyte</summary>
   let kibibyte = 1024L

   ///<summary>Represents 1 Mebibyte</summary>
   let mebibyte = 1024L * kibibyte

   ///<summary>Represents 1 Gibibyte</summary>
   let gibibyte = 1024L * mebibyte

   ///<summary>Represents 1 Tebibyte</summary>
   let tebibyte = 1024L * gibibyte

   ///<summary>Convenience active pattern for turning bytes into kibibytes
   let (|KibiBytes|) (bytes : int64) =
      match bytes with
      | 0L -> 0L
      | bytes -> bytes / kibibyte |> BytesOr1

   ///<summary>Convenience active pattern for turning bytes into mebibytes</summary>
   let (|MebiBytes|) (bytes : int64) = 
      match bytes with
      | 0L -> 0L
      | bytes ->  bytes / mebibyte |> BytesOr1

    ///<summary>Convenience active pattern for turning bytes into gibibytes</summary>
   let (|GibiBytes|) (bytes : int64) =
      match bytes with
      | 0L -> 0L
      | bytes -> bytes / gibibyte |> BytesOr1

    ///<summary>Convenience active pattern for turning bytes into tebibytes</summary>
   let (|TebiBytes|) (bytes : int64) =
      match bytes with
      | 0L -> 0L
      | bytes -> bytes / tebibyte |> BytesOr1

   ///<summary>Prints the message to the console with the current time</summary>
   let PrintToConsole msg = printfn "[%A] - %A" DateTime.Now msg

   ///<summary>Prints a newline to the console</summary>
   let PrintNewLineToConsole() = printfn ""
         
///<summary>A module with constants defined for the program</summary>
module Constants =
   let internal FileManifestFileName = "ARCHIVE_FILE_MANIFEST.txt"   

module Seq =
   ///<summary>Turns one item into a sequence</summary>
   let internal ToSeq item = seq { yield item }

   ///<summary>Append one item to the end of a sequence</summary>
   let internal AppendItem seq item = Seq.append seq (ToSeq item)
