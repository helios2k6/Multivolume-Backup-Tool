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

   let WithByteMeasure x = x * 1L<byte>

   let WithKibibyteMeasure x = x * 1L<kibibyte>

   let WithMebibyteMeasure x = x * 1L<mebibyte>

   let WithGibibyteMeasure x = x * 1L<gibibyte>

   let WithTebibyteMeasure x = x * 1L<tebibyte>
   
   let WithoutMeasure (x : int64<_>) = int64(x)

   let bytesPerKibibyte = 1024L<byte/kibibyte>

   let bytesPerMebibyte = 1024L<kibibyte/mebibyte> * bytesPerKibibyte

   let bytesPerGibibyte = 1024L<mebibyte/gibibyte> * bytesPerMebibyte

   let bytesPerTebibyte = 1024L<gibibyte/tebibyte> * bytesPerGibibyte

   let kibibytesPerMebibyte = 1024L<kibibyte/mebibyte>

   let mebibytesPerGibibyte = 1024L<mebibyte/gibibyte>

   let gibibytesPerTebibyte = 1024L<gibibyte/tebibyte>

   //Transformations
   //Upward Transformations
   let BytesToKibibytes (x : int64<byte>) = x / bytesPerKibibyte

   let BytesToMebibytes (x : int64<byte>) = BytesToKibibytes x / kibibytesPerMebibyte

   let BytesToGibibytes (x : int64<byte>) = BytesToMebibytes x / mebibytesPerGibibyte

   let BytesToTebibytes (x : int64<byte>) = BytesToGibibytes x / gibibytesPerTebibyte
   
   //Downward Transformations
   let GibibytesToMebibytes (x : int64<gibibyte>) = x * mebibytesPerGibibyte

   let MebibytesToKibibytes (x : int64<mebibyte>) = x * kibibytesPerMebibyte

   let KibibytesToBytes (x : int64<kibibyte>) = x * bytesPerKibibyte

   let GibibytesToKibibytes (x : int64<gibibyte>) = GibibytesToMebibytes >> MebibytesToKibibytes <| x

   let GibibytesToBytes (x : int64<gibibyte>) = GibibytesToKibibytes >> KibibytesToBytes <| x

   let MebibytesToBytes (x : int64<mebibyte>) = MebibytesToKibibytes >> KibibytesToBytes <| x

   let MebibytesOr1 (x : int64<mebibyte>) = if x = 0L<mebibyte> then 1L<mebibyte> else x

module MathHelpers =
   let Max (a : int64<'a>) (b : int64<'a>) = if a >= b then a else b

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

module Seq =
   ///<summary>Applies a predicate to the sequence to see if every item fulfills the predicate. Empty sequences return true!</summary>
   let internal All predicate seq = 
      let foldFunc status item = if status && predicate(item) then true else false
      seq |> Seq.fold foldFunc true

   ///<summary>Unwraps a sequence of optional values into their raw values</summary>
   let internal UnwrapOptionalSeq inSeq =
      let chooser input = 
         match input with 
         | Some(_) -> true
         | _ -> false

      let mapper input =
         match input with
         | Some(x) -> x
         | _ -> failwith "Impossible"

      inSeq
      |> Seq.filter chooser
      |> Seq.map mapper

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

