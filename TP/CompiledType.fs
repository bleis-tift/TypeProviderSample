module CompiledType

open Microsoft.FSharp.Compiler.CodeDom
open System.Reflection
open System.CodeDom.Compiler

type t<'a> =
| CompileError of CompilerError seq
| Result of 'a

let compile moduleName src =
  use provider = new FSharpCodeProvider()
  let dll = System.IO.Path.GetTempFileName() + ".dll"
  let src = "module " + moduleName + "\nopen System\n" + src
  let param = CompilerParameters(OutputAssembly=dll, CompilerOptions="--target:library")//(GenerateInMemory=true)
  let res = provider.CompileAssemblyFromSource(param, [| src |])
  let errors = res.Errors |> Seq.cast<CompilerError>
  if not(Seq.isEmpty errors) then
    CompileError errors
  else
    let asm = res.CompiledAssembly
    let t = asm.GetType(moduleName)
    Result t