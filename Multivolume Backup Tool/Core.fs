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

///<summary>A module with constants defined for the program</summary>
module Constants =
   /// <summary>
   /// The file name of the file manifest
   /// </summary>
   let internal FileManifestFileName = "ARCHIVE_FILE_MANIFEST.txt"

module Seq = 
   /// <summary>
   /// Append an item to the end of a sequence
   /// </summary>
   let append item sequence = seq { yield! sequence; yield item; }

   /// <summary>
   /// Form a new sequence of elements where any element in first
   /// that doesn't appear in second is yielded
   /// </summary>
   let except first second = 
      let setOfSecond = Set.ofSeq second

      seq {
         for e in first do
         if setOfSecond.Contains e then yield e
      }

module Math =
   /// <summary>
   /// Calculates the maximum between two unit integers
   /// </summary>
   /// <param name="a">The first number</param>
   /// <param name="b">The second number</param>
   let internal max (a : int64<'a>) (b : int64<'a>) = if a >= b then a else b

module IO =
   /// <summary>
   /// Represents a file
   /// </summary>
   type FileEntry(filePath : string) =
      let fileInfo = lazy new FileInfo(filePath)

      ///<summary>Get the FileInfo object associated with this FileEntry</summary>
      member this.Info with get() = fileInfo.Value

      ///<summary>Get the path passed into this FileEntry</summary>
      member this.Path with get() = this.Info.FullName

      ///<summary>Get the size of this FileEntry in bytes</summary>
      member this.Size with get() = this.Info.Length |> Measure.WithByteMeasure

      interface IComparable<FileEntry> with
         member this.CompareTo other = this.Path.CompareTo other.Path
      end

      interface IComparable with
         member this.CompareTo other = 
            match other with
            | :? FileEntry as fileEntry -> this.Path.CompareTo(fileEntry.Path)
            | _ -> -1
      end

      interface IEquatable<FileEntry> with
         member this.Equals other = this.Path.Equals(other.Path)
      end

      override this.Equals other =
         match other with
         | :? FileEntry as entry -> this.Path.Equals(entry.Path)
         | _ -> false

      override this.GetHashCode() = this.Path.GetHashCode()

      override this.ToString() = this.Path