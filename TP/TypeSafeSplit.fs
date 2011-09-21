namespace StringUtil

open System
open System.Collections.Generic
open System.Linq.Expressions
open System.Globalization
open System.Reflection
open System.Diagnostics
open Microsoft.FSharp.Core.CompilerServices
open System.Text.RegularExpressions

[<assembly: TypeProviderAssembly>]
do()

type Splitter = class end

[<TypeProvider>]
type SplitterProvider() =
  let invalidation = Event<EventHandler, EventArgs>()
  interface IProvidedNamespace with
    member this.ResolveTypeName(typeName) = typeof<Splitter>
    member this.NamespaceName with get() = "StringUtil"
    member this.GetNestedNamespaces() = Array.empty
    member this.GetTypes() = [| typeof<Splitter> |]
  interface ITypeProvider with
    member this.GetNamespaces() = [| this |]
    member this.Dispose() = ()
    [<CLIEvent>]
    member this.Invalidate = invalidation.Publish
    member this.GetStaticParameters(typeWithoutArguments) =
      [| { new ParameterInfo() with
             member x.Name = "count"
             member x.ParameterType = typeof<int> } |]
    member this.ApplyStaticArguments(typeWithoutArgs, typeNameWithArgs, staticArgs) =
      let makeSplit name count =
        let pat = List.init count (fun i -> "s" + (string i)) |> String.concat "; "
        let tpl = List.init count (fun i -> "s" + (string i)) |> String.concat ", "
        let f = "let split =\n" +
                "  let splitImpl (sep: string) (str: string) =\n" +
                "    match str.Split([| sep |], " + (string count) + ", StringSplitOptions.None) with\n" +
                "    | [| " + pat + " |] -> (" + tpl + ")\n" +
                "    | _ -> failwith \"not match.\"\n" +
                "  splitImpl"
        match CompiledType.compile name f with
        | CompiledType.Result t -> t
        | CompiledType.CompileError e -> failwith "oops!"
      let r = makeSplit typeNameWithArgs (staticArgs.[0] :?> int)
      r
    member this.GetInvokerExpression(syntheticMethodBase, parameters) =
      let m = syntheticMethodBase :?> MethodInfo
      let args = parameters |> Seq.cast<Expression>
      Expression.Call(null, m, args) :> Expression