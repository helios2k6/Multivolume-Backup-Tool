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
   let private (|MebiByteOr1|) mebibyte = if mebibyte = 0L then 1L else mebibyte

   ///<summary>Convenience active pattern for turning bytes into kibibytes</summary>
   let (|MebiBytes|) (bytes : int64) = 
      match bytes with
      | 0L -> 0L
      | bytes ->  bytes / 1048576L |> (|MebiByteOr1|)
         
///<summary>A module with constants defined for the program</summary>
module Constants =
   let internal FileManifestFileName = "ARCHIVE_FILE_MANIFEST.txt"   

module Seq =
   ///<summary>Turns one item into a sequence</summary>
   let internal ToSeq item = seq { yield item }

   ///<summary>Append one item to the end of a sequence</summary>
   let internal AppendItem seq item = Seq.append seq (ToSeq item)
