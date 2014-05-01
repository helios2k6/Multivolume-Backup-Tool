namespace MBT

open MBT
open MBT.Core
open MBT.Messages
open System

type BackupErrorActor(parent : IActor) =
   inherit