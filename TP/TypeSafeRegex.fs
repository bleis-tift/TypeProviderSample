module TypeSafeRegex

open System.Text.RegularExpressions
open Microsoft.FSharp.Core.CompilerServices
open TPUtil
open TPUtil.SimpleTypeProvider
open TPUtil.StaticArgument

type TSRegex() =
  static member GenSrc args =
    let pattern = (args |> List.find (fun a -> a.Name = "pattern")).Value :?> string
    let r = Regex(pattern)
    let groups = r.GetGroupNumbers()
    let len = groups.Length
    let tpl =
      groups
      |> Seq.skip 1
      |> Seq.map (sprintf "(groups.[%d].Value)")
      |> String.concat ", "
    "let match' =\n  \
       let matchImpl str =\n    \
         let r = Regex(\"" + pattern + "\")\n    \
         let groups = r.Match(str).Groups\n    \
         if groups.Count = " + (string len) + " then\n      \
           Some (" + tpl + ")\n    \
         else\n      \
           None\n  \
       matchImpl"
    
[<TypeProvider>]
type TypeSafeRegexProvider() =
  inherit SimpleTypeProviderBase begin
    { NameSpace = "RegexUtil"
      ProvideType = typeof<TSRegex>
      StaticParams = [ StaticParameter.make "pattern" typeof<string> ]
      OpenModules = [ "System"; "System.Text.RegularExpressions" ]
      GenSrc = TSRegex.GenSrc }
  end