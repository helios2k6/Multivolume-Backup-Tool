﻿(*
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
   open System.Collections.Generic

   /// <summary>
   /// Append an item to the end of a sequence
   /// </summary>
   let appendItem item (sequence : 'a seq) = 
      match sequence with
      | :? IList<'a> as list -> 
         list.Add item 
         list :> 'a seq
      | _ ->
         let list = new List<'a>(sequence)
         list.Add item
         list :> 'a seq

   /// <summary>
   /// Form a new sequence of elements where any element in first
   /// that doesn't appear in second is yielded
   /// </summary>
   let except first second = 
      let setOfSecond = Set.ofSeq second

      seq {
         for e in first do
         if not <| setOfSecond.Contains e then yield e
      }

module Map = 
   open System.Collections.Generic

   /// <summary>
   /// Retrieves the keys of the map
   /// </summary>
   /// <param name="map">The map</param>
   let internal keys map = 
      let folder state key _ = Seq.appendItem key state

      Map.fold folder Seq.empty map

   /// <summary>
   /// Retrieves the values of the map
   /// </summary>
   /// <param name="map">The map</param>
   let internal values map =
      let folder state _ value = Seq.appendItem value state

      Map.fold folder Seq.empty map

   /// <summary>
   /// Reverses the mappings. This will destroy any many-to-1 mappings. Use with caution
   /// </summary>
   /// <param name="map">The map</param>
   let internal reverse map = Map.fold (fun revMap key value -> Map.add value key revMap) Map.empty map

   /// <summary>
   /// Converts a map to a dictionary
   /// </summary>
   /// <param name="map">The map to convert</param>
   let internal convertToDictionary map =
      let dict = new Dictionary<_,_>()
      Map.iter (fun key value -> dict.[key] <- value) map
      dict

   /// <summary>
   /// Remaps the keys of a map using a func to project the previous set of
   /// keys into another domain
   /// </summary>
   /// <param name="map">The map</param>
   /// <param name="remapper">The key -> key' projection</param>
   let internal remapKeys remapper (map : Map<_,_>) =
      let mapper (kvp : KeyValuePair<_,_>) =
         let key = remapper kvp.Key
         let value = kvp.Value

         (key, value)

      Seq.map mapper map |> Map.ofSeq

module Math =
   /// <summary>
   /// Calculates the maximum between two unit integers
   /// </summary>
   /// <param name="a">The first number</param>
   /// <param name="b">The second number</param>
   let internal max (a : int64<'a>) (b : int64<'a>) = if a >= b then a else b

module IO =
   type private FileMetadata = { Path : string; Size : int64<Measure.byte> }

   /// <summary>
   /// Represents a file
   /// </summary>
   type FileEntry(filePath : string) =
      let initializeFileMetadata() =
         let fileInfo = new FileInfo(filePath)
         { Path = fileInfo.FullName; Size = fileInfo.Length |> Measure.WithByteMeasure}

      let fileMetadata = lazy initializeFileMetadata()

      ///<summary>Get the path passed into this FileEntry</summary>
      member __.Path with get() = fileMetadata.Value.Path

      ///<summary>Get the size of this FileEntry in bytes</summary>
      member __.Size with get() = fileMetadata.Value.Size

      /// <summary>
      /// Forces the computation of all lazily initialized values
      /// </summary>
      member __.Compute() = fileMetadata.Force() |> ignore

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