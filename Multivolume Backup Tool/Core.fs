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
open System.Collections.Generic
open System.IO

///<summary>A placeholder type for the "Unit" type for generic parameters</summary>
type UnitPlaceHolder = Hold

///<summary> Units of measure</summary>
module Measure =
   [<Measure>]
   type byte
   
   [<Measure>]
   type kibibyte
   
   [<Measure>]
   type mebibyte

   [<Measure>]
   type gibibyte

   [<Measure>]
   type tebibyte

   ///<summary>Discriminate union to wrap the data types
   type DataMetric = 
      | Bytes of int64<byte> 
      | Kibibytes of int64<kibibyte> 
      | Mebibytes of int64<mebibyte>
      | Gibibytes of int64<gibibyte>
      | Tebibytes of int64<tebibyte>

   let WithByteMeasure x = x * 1L<byte>

   let WithKibibyteMeasure x = x * 1L<kibibyte>

   let WithMebibyteMeasure x = x * 1L<mebibyte>

   let WithGibibyteMeasure x = x * 1L<gibibyte>

   let WithTebibyteMeasure x = x * 1L<tebibyte>
   
   let WithoutMeasure (x : int64<_>) = int64(x)

   let bytesPerKibibyte = 1024L<byte/kibibyte>

   let bytesPerMebibyte = 1024L<kibibyte> * bytesPerKibibyte

   let bytesPerGibibyte = 1024L<mebibyte> * bytesPerMebibyte

   let bytesPerTebibyte = 1024L<gibibyte> * bytesPerGibibyte

   let kibibytesPerMebibyte = 1024L<kibibyte/mebibyte>

   let mebibytesPerGibibyte = 1024L<mebibyte/gibibyte>

   let gibibytesPerTebibyte = 1024L<gibibyte/tebibyte>

   //Transformations
   //Upward Transformations
   let BytesToKibibytes (x : int64<byte>) = x / bytesPerKibibyte

   let BytesToMebibytes (x : int64<byte>) = x / bytesPerMebibyte * 1L<mebibyte>

   let BytesToGibibytes (x : int64<byte>) = x / bytesPerGibibyte * 1L<gibibyte>

   let BytesToTebibytes (x : int64<byte>) = x / bytesPerTebibyte * 1L<tebibyte>
   
   //Downward Transformations
   let GibibytesToMebibytes (x : int64<gibibyte>) = x * mebibytesPerGibibyte
   
   let MebibytesOr1 (x : int64<mebibyte>) = if x = 0L<mebibyte> then 1L<mebibyte> else x

///<summary>Utility module</summary>
module Utilities =
   let private LockObject = new Object()

   ///<summary>Prints the message to the console with the current time</summary>
   let PrintToConsole msg = lock LockObject (fun() -> String.Format("[{0}] - {1}", DateTime.Now, msg) |> Console.WriteLine )

   ///<summary>Prints a newline to the console</summary>
   let PrintNewLineToConsole() = lock LockObject (fun() -> printfn "")
         
///<summary>A module with constants defined for the program</summary>
module Constants =
   let internal FileManifestFileName = "ARCHIVE_FILE_MANIFEST.txt"   

module Predicates =
   ///<summary>Checks an optional to see if it's Some value</summary>
   let internal IsSome opt = 
      match opt with
      | Some(_) -> true
      | None -> false

module Seq =
   ///<summary>Applies a predicate to the sequence to see if every item fulfills the predicate. Empty sequences return true!</summary>
   let internal All predicate seq = 
      let foldFunc status item = if status && predicate(item) then true else false
      seq |> Seq.fold foldFunc true

   ///<summary>Gets the tail of the sequence, or the empty sequence if the sequence is empty</summary>
   let internal Tail seq = 
      if Seq.isEmpty seq then
         Seq.empty
      else
         Seq.skip 1 seq

   ///<summary>Gets the head and tail of a sequence</summary>
   let internal (|HeadAndTail|) aSeq = (Seq.head aSeq, Tail aSeq)

   ///<summary>Unwraps a sequence of optional values into their raw values</summary>
   let internal UnwrapOptionalSeq inSeq = 
      let rec generateSeqOnSomeOnly state remainingSeq =
         if Seq.isEmpty remainingSeq then
            state
         else
            let head, tail = (|HeadAndTail|) remainingSeq
            match head with
            | Some(a) -> generateSeqOnSomeOnly (seq { yield! state; yield a }) tail
            | None -> generateSeqOnSomeOnly state tail

      generateSeqOnSomeOnly Seq.empty inSeq

module Tuple =
   ///<summary>Takes the first item of a 3-tuple</summary>
   let fstOfThree tuple = 
      match tuple with 
      (a, _, _) -> a

   let sndOfThree tuple =
      match tuple with
      (_, b, _) -> b

   ///<summary>Takes the third item of a 2-tuple</summary>
   let thrdOfThree tuple =
      match tuple with
      (_, _, c) -> c

