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

open System.IO

/// <summary>
/// The writer actor's state
/// </summary>
type internal WriterState = { Streams: Map<string, FileStream>; Completed : Set<string> }

/// <summary>
/// The file writer actor
/// </summary>
type internal Writer() =
   inherit BaseStateActor<WriterState>({ Streams = Map.empty; Completed = Set.empty })

   (* Private functions *)
   let processPayload state payload =
      let streamOpt = state.Streams.TryFind payload.Path
      
      let fileStream = 
         match streamOpt with
         | Some(fileStream) -> fileStream
         | None -> new FileStream(payload.Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)

      fileStream.Write(payload.Payload, 0, payload.Payload.Length)
      
      if payload.IsEnd then 
         fileStream.Dispose()
         { Streams = Map.remove payload.Path state.Streams; Completed = Set.add payload.Path state.Completed }
      else
         { state with Streams = Map.add payload.Path fileStream state.Streams }
         
   (* Public methods *)
   override __.ProcessMessage state msg = 
      match msg with
      | Write(payload) -> processPayload state payload
      | _ -> failwith "Unknown message"