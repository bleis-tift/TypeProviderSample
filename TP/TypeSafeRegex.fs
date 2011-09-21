module TypeSafeRegex

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

type TSRegex = class end

[<TypeProvider>]
type SplitterProvider() =
  let invalidation = Event<EventHandler, EventArgs>()
  interface IProvidedNamespace with
    member this.ResolveTypeName(typeName) = typeof<TSRegex>
    member this.NamespaceName with get() = "RegexUtil"
    member this.GetNestedNamespaces() = Array.empty
    member this.GetTypes() = [| typeof<TSRegex> |]
  interface ITypeProvider with
    member this.GetNamespaces() = [| this |]
    member this.Dispose() = ()
    [<CLIEvent>]
    member this.Invalidate = invalidation.Publish
    member this.GetStaticParameters(typeWithoutArguments) =
      [| { new ParameterInfo() with
             member x.Name = "pattern"
             member x.ParameterType = typeof<string> } |]
    member this.ApplyStaticArguments(typeWithoutArgs, typeNameWithArgs, staticArgs) =
      let makeRegex name pattern =
        let r = Regex(pattern)
        let groups = r.GetGroupNumbers()
        let len = groups.Length
        let tpl =
          groups
          |> Seq.skip 1
          |> Seq.map (fun i -> sprintf "(groups.[%d].Value)" i)
          |> String.concat ", "
        let src = "let match' =\n" +
                  "  let matchImpl str =\n" +
                  "    let r = Regex(\"" + pattern + "\")\n" +
                  "    let groups = r.Match(str).Groups\n" +
                  "    if groups.Count = " + (string len) + " then\n" +
                  "      Some (" + tpl + ")\n" +
                  "    else\n" +
                  "      None\n" +
                  "  matchImpl"
        match CompiledType.compile ["System"; "System.Text.RegularExpressions"] name src with
        | CompiledType.Result t -> t
        | CompiledType.CompileError e -> failwith (e |> Seq.head |> string)
      let r = makeRegex typeNameWithArgs (staticArgs.[0] :?> string)
      r
    member this.GetInvokerExpression(syntheticMethodBase, parameters) =
      let m = syntheticMethodBase :?> MethodInfo
      let args = parameters |> Seq.cast<Expression>
      Expression.Call(null, m, args) :> Expression