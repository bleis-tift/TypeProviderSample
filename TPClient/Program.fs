[<EntryPoint>]
let main _ =
  TypeSafeSplitSample.sample1 () // a="hoge", b=" piyo, foo, bar"
  TypeSafeSplitSample.sample2 () // not match.

  TypeSafeRegexSample.sample1 () // 13902
  TypeSafeRegexSample.sample2 () // not match.
  TypeSafeRegexSample.sample3 ()
  0