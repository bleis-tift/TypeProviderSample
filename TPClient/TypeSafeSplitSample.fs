module TypeSafeSplitSample

open StringUtil

// 2要素のタプルに分解する型
[<Generate>]
type Split2 = TSSplit<2>

// 100要素のタプルに分解する型
// コード生成でこれをやろうとすると、
// 生成されたコードが大きくなりすぎるのに比べ、
// TypeProviderは生成されるコードが無いのが強み
[<Generate>]
type Split100 = TSSplit<100>

// 型名はなんだっていい
[<Generate>]
type Hoge = TSSplit<3>

// 分解できる例
let sample1 () =
  let a, b = "hoge, piyo, foo, bar" |> Split2.split ","
  printfn "a=%A, b=%A" a b

// 分解できない例
let sample2 () =
  try
    let _ = "hoge, piyo, foo, bar" |> Split100.split ","
    // ここには来ない
    exit 1
  with
    e -> printfn "%s" e.Message

// Type mismatch. Expecting a string -> string * string
//                but given a string -> string * string * string 
// The tuples have differing lengths of 2 and 3
(* 下のコメントアウトを外すと、上記のようなコンパイルエラーになる *)
//let sample3 () =
//  let a, b = "hoge, piyo, foo, bar" |> Hoge.split ","
//  printfn "a=%A, b=%A" a b