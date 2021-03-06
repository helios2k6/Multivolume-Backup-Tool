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

open Actors

module private Impl =
   /// <summary>
   /// Private call. Used by base class actors to determine 
   /// whether a message is the shutdown message
   /// </summary>
   /// <param name="msg"></param>
   let isShutdownMessage msg = 
      match msg with
      | Shutdown -> true
      | _ -> false

open Impl

/// <summary>
/// The base class for any state machine actors in this actor system
/// </summary>
[<AbstractClass>]
type internal BaseStateActor<'state>(initialState : 'state) =
   inherit StateActor<Message, 'state>(initialState)

   override __.IsShutdownMessage msg = isShutdownMessage msg

/// <summary>
/// The base class for any stateless actors in this actor system
/// </summary>
[<AbstractClass>]
type internal BaseStatelessActor() =
   inherit StatelessActor<Message>()

   override __.IsShutdownMessage msg = isShutdownMessage msg