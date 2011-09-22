module TypeSafeRegexSample

open RegexUtil

// <と@をつなげるとパースできないので、離すこと
// この正規表現はコンパイル時に文法がチェックされるので、
// )を取るとか色々としてみると面白い
[<Generate>]
type PhoneNumberPat = TSRegex< @"(\d{3})-(\d{4})-(\d{4})" >

// マッチするパターン
// マッチしたグループをタプルとして取り出せる！
let sample1 () =
  match "123-4678-9101" |> PhoneNumberPat.match' with
  | Some(a, b, c) -> (int a) + (int b) + (int c) |> printfn "%d"
  | None -> failwith "oops!"

// マッチしないパターン
let sample2 () =
  match "123-45678-910a" |> PhoneNumberPat.match' with
  | Some _ -> failwith "oops!"
  | None -> printfn "not match."

// 名前付きグループ
[<Generate>]
type UrlPat = TSRegex< @"(?<Scheme>[^:]+)://(?<Host>[^/]+)(?<Path>/.+)" >

let sample3 () =
  let result = "http://example.jp/hoge/piyo/foo/bar" |> UrlPat.nameMatch
  printfn "%A" result