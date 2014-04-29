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

open System

///<summary>
///The application configuration according to the command line 
///</summary>
type ApplicationConfiguration = { 
      ///<summary>The archive file path</summary>
      ArchiveFilePath : String; 
      ///<summary>The folders to archive</summary>
      Folders : seq<String>; 
      ///<summary>The list of files to ignore</summary>
      Blacklist : seq<String>; 
      ///<summary>The list of files to include</summary>
      Whitelist : seq<String>; 
   }

///<summary>A factory class for creating ApplicationConfiguration records</summary>
type ApplicationConfigurationFactory =

   ///<summary>Creates an ApplicationConfiguration record with a CommandLineArgument object</summary>
   static member public CreateConfiguration (commandLineArgs : CommandLineArguments) = {
         ArchiveFilePath = commandLineArgs.ArchiveFilePath; 
         Folders = commandLineArgs.Folders; 
         Blacklist = commandLineArgs.Blacklist; 
         Whitelist = commandLineArgs.Whitelist
      }
