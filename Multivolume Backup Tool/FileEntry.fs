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
open System.IO

///<summary>Represents a File entry</summary>
type FileEntry(filePath : String) =
   let fileInfo = new FileInfo(filePath)

   ///<summary>Get the FileInfo object associated with this FileEntry</summary>
   member this.Info with get() = fileInfo

   ///<summary>Get the path passed into this FileEntry</summary>
   member this.Path with get() = this.Info.FullName

   interface IComparable<FileEntry> with
      member this.CompareTo other = filePath.CompareTo other.Path
   end

   interface IComparable with
      member this.CompareTo other = 
         match other with
         | :? FileEntry as fileEntry -> filePath.CompareTo(fileEntry.Path)
         | _ -> -1
   end

   interface IEquatable<FileEntry> with
      member this.Equals other = filePath.Equals(other.Path)
   end

   override this.Equals other =
      match other with
      | :? FileEntry as entry -> filePath.Equals(entry.Path)
      | _ -> false

   override this.GetHashCode() = filePath.GetHashCode()

   override this.ToString() = this.Path