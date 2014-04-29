namespace MBT

open System
open CommandLine
open CommandLine.Text

///<summary>
/// Represents the command line arguments
///</summary>
type CommandLineArguments() =
   ///<summary>
   ///The path to the main archive
   ///</summary>
   [<OptionArray("archive-path", Required = true, HelpText = "The path to the main archive file")>]
   member val ArchiveFilePath = String.Empty with get, set

   ///<summary>
   ///The folders you want to archive
   ///</summary>
   [<OptionArray("folders", Required = true, HelpText = "The folders to archive")>]
   member val Folders = Array.empty<String> with get, set

   ///<summary>
   ///The black list of files
   ///</summary>
   [<OptionArray("blacklist", Required = false, HelpText = "The files to blacklist. If only a blacklist is supplied, then all files except those on the blacklist will be archived. If a blacklist and a whitelist are supplied, then the blacklist is consulted first. Providing neither will accept all files")>]
   member val Blacklist = Array.empty<String> with get, set
   
   ///<summary>
   ///The white list of files
   ///</summary>
   [<OptionArray("whitelist", Required = false, HelpText = "The files to whitelist. If only a whiltelist is supplied, then no files except those on the whitelist will be archived. If a whitelist and a blacklist are supplied, then the blacklist is consulted first. Providing neither will accept all files")>]
   member val Whitelist = Array.empty<String> with get, set
   
   ///<summary>
   ///The parser state. Warning, this can be null because we're accessing a C# library
   ///</summary>
   [<ParserState>]
   member val State = Unchecked.defaultof<IParserState> with get, set

   ///<summary>
   ///Whether or not to show the help message
   ///</summary>
   [<HelpOption(HelpText = "Display this help message")>]
   member public this.Help() = HelpText.AutoBuild(this, (fun item -> HelpText.DefaultParsingErrorsHandler(this, item)))