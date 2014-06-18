namespace MBT
namespace MBT.Core

type MaybeMonad() =
   member this.Bind(x, f) =
      match x with
      | Some(x) -> f(x)
      | _ -> None

   member this.Delay f = f()
   member this.Return x = Some x

module Monads =
   ///<summary>The maybe monad</summary>
   let internal maybe = new MaybeMonad()