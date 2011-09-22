namespace TPUtil

open System
open System.Reflection
open System.Linq.Expressions
open Microsoft.FSharp.Core.CompilerServices
open CompiledType

[<assembly: TypeProviderAssembly>]
do()

module StaticParameter =
  type t = {
    Name: string
    ParameterType: Type
  }
  let make<'ParamType> name = { Name = name; ParameterType = typeof<'ParamType> }

module StaticArgument =
  type t = {
    Name: string
    ArgumentType: Type
    Value: obj
  }
  let from staticParams values =
    let convert ({ StaticParameter.Name = n; StaticParameter.ParameterType = t }, v) =
      { Name = n; ArgumentType = t; Value = v }

    List.zip staticParams values
    |> List.map convert

open StaticParameter
open StaticArgument

module SimpleTypeProvider =
  [<AbstractClass>]
  type SimpleTypeProviderBase<'ProvideType>(NameSpace) =
    let invalidation = Event<EventHandler, EventArgs>()
    member val StaticParams: StaticParameter.t list = [] with get, set
    member val OpenModules = [ "System" ] with get, set
    abstract GenSrc: StaticArgument.t list -> string
    interface IProvidedNamespace with
      member this.ResolveTypeName(typeName) = typeof<'ProvideType>
      member this.NamespaceName with get() = NameSpace
      member this.GetNestedNamespaces() = Array.empty
      member this.GetTypes() = [| typeof<'ProvideType> |]
    interface ITypeProvider with
      member this.GetNamespaces() = [| this |]
      member this.Dispose() = ()
      [<CLIEvent>]
      member this.Invalidate = invalidation.Publish

      member this.GetStaticParameters(typeWithoutArguments) =
        let recToClass pos { Name = n; ParameterType = pt } =
          { new ParameterInfo() with
              member x.Name = n
              member x.ParameterType = pt
              member x.Position = pos }
        this.StaticParams |> List.mapi recToClass |> List.toArray

      member this.ApplyStaticArguments(typeWithoutArgs, typeNameWithArgs, staticArgs) =
        let staticArgs = staticArgs |> Array.toList
        let src = this.GenSrc (staticArgs |> StaticArgument.from this.StaticParams)
        match CompiledType.compile this.OpenModules typeNameWithArgs src with
        | Result t -> t
        | CompileError e -> failwith (e |> Seq.head |> string)

      member this.GetInvokerExpression(syntheticMethodBase, parameters) =
        let m = syntheticMethodBase :?> MethodInfo
        let args = parameters |> Seq.cast<Expression>
        Expression.Call(null, m, args) :> Expression