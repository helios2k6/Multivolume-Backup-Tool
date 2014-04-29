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
