module CompiledType

open System.IO
open System.Reflection
open System.CodeDom.Compiler
open Microsoft.FSharp.Compiler.CodeDom

type t<'a> =
| CompileError of CompilerError seq
| Result of 'a

let compile loadings moduleName src =
  use provider = new FSharpCodeProvider()
  let src =
    let openMods = loadings |> List.map (sprintf "open %s\n") |> String.concat ""
    "module " + moduleName + "\n" + openMods + src
  let param =
    let dll = Path.GetTempFileName() + ".dll"
    CompilerParameters([| "System.dll" |], OutputAssembly=dll, CompilerOptions="--target:library")
  let res = provider.CompileAssemblyFromSource(param, [| src |])
  let errors = res.Errors |> Seq.cast<CompilerError>
  if not(Seq.isEmpty errors) then
    CompileError errors
  else
    let asm = res.CompiledAssembly
    let t = asm.GetType(moduleName)
    Result t