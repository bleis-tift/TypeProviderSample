module TypeSafeRegex

open System.Text.RegularExpressions
open Microsoft.FSharp.Core.CompilerServices
open TPUtil
open TPUtil.SimpleTypeProvider
open TPUtil.StaticArgument

type TSRegex = class end
    
[<TypeProvider>]
type TypeSafeRegexProvider() =
  inherit SimpleTypeProviderBase<TSRegex> begin
    NameSpace = "RegexUtil",
    StaticParams = [ StaticParameter.make<string> "pattern" ],
    OpenModules = [ "System"; "System.Text.RegularExpressions" ]
  end

  override this.GenSrc args =
    let indexBaseMatch pattern (r: Regex) =
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
         matchImpl\n"
    let nameBaseMatch pattern (r: Regex) =
      let isInt = String.forall (fun c -> '0' <= c && c <= '9')
      let memberize g = if g |> isInt then "_" + g else g

      let groups = r.GetGroupNames()
      let records = groups |> Seq.skip 1
      let recordMember = records |> Seq.map (memberize >> sprintf "%s: Group") |> String.concat "; "
      let recordAssign = records |> Seq.map (fun g -> sprintf "%s = groups.[\"%s\"]" (memberize g) g) |> String.concat "; "
      "type t = { " + recordMember + " }\n\
       let nameMatch =\n  \
         let nameMatch' str =\n    \
           let r = Regex(\"" + pattern + "\")\n    \
           let groups = r.Match(str).Groups\n    \
           { " + recordAssign + " }\n  \
         nameMatch'\n"

    let pattern = (args |> List.find (fun a -> a.Name = "pattern")).Value :?> string
    let r = Regex(pattern)
    (indexBaseMatch pattern r) + (nameBaseMatch pattern r)