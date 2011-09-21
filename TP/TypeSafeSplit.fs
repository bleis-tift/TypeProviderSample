namespace StringUtil

open Microsoft.FSharp.Core.CompilerServices
open TPUtil
open TPUtil.SimpleTypeProvider
open TPUtil.StaticArgument

type TSSplit() =
  static member GenSrc args =
    let count = (args |> List.find (fun a -> a.Name = "count")).Value :?> int
    let vars = List.init count (fun i -> "s" + (string i))
    let pat = vars |> String.concat "; "
    let tpl = vars |> String.concat ", "
    "let split =\n  \
       let split' (sep: string) (str: string) =\n    \
         match str.Split([| sep |], " + (string count) + ", StringSplitOptions.None) with\n    \
         | [| " + pat + " |] -> (" + tpl + ")\n    \
         | _ -> failwith \"not match.\"\n  \
       split'"
    
[<TypeProvider>]
type SplitterProvider() =
  inherit SimpleTypeProviderBase begin
    { NameSpace = "StringUtil"
      ProvideType = typeof<TSSplit>
      StaticParams = [ StaticParameter.make "count" typeof<int> ]
      OpenModules = [ "System" ]
      GenSrc = TSSplit.GenSrc }
  end