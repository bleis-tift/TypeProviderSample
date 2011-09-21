namespace TP

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
  let make name paramType = { Name = name; ParameterType = paramType }

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
  type Info = {
    NameSpace: string
    ProvideType: Type
    StaticParams: StaticParameter.t list
    GenSrc: StaticArgument.t list -> string
    OpenModules: string list
  }

  type SimpleTypeProviderBase(info: Info) =
    let invalidation = Event<EventHandler, EventArgs>()
    interface IProvidedNamespace with
      member this.ResolveTypeName(typeName) = info.ProvideType
      member this.NamespaceName with get() = info.NameSpace
      member this.GetNestedNamespaces() = Array.empty
      member this.GetTypes() = [| info.ProvideType |]
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
        info.StaticParams |> List.mapi recToClass |> List.toArray

      member this.ApplyStaticArguments(typeWithoutArgs, typeNameWithArgs, staticArgs) =
        let staticArgs = staticArgs |> Array.toList
        let src = info.GenSrc (staticArgs |> StaticArgument.from info.StaticParams)
        match CompiledType.compile info.OpenModules typeNameWithArgs src with
        | Result t -> t
        | CompileError e -> failwith (e |> Seq.head |> string)

      member this.GetInvokerExpression(syntheticMethodBase, parameters) =
        let m = syntheticMethodBase :?> MethodInfo
        let args = parameters |> Seq.cast<Expression>
        Expression.Call(null, m, args) :> Expression