[<AutoOpen>]
module Util

open System
open System.Text.RegularExpressions

let (|ParseInt|_|) s =
    match Int32.TryParse s with
    | true, i -> Some i
    | _ -> None

let (|IgnoreCase|_|) compareTo s =
    match String.Equals(compareTo, s, StringComparison.OrdinalIgnoreCase) with
    | true -> Some ()
    | false -> None

let (|RegexMatch|_|) pattern s =
    match Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase) with
    | true -> Some s
    | false -> None

let (|RegexCapture|_|) pattern s =
    match s with
    | null -> Option.None
    | _ ->
        let mtch = Regex.Match (s, pattern, RegexOptions.IgnoreCase)
        match mtch.Success with
        | true -> [ for group in mtch.Groups do yield group.Value ] |> Some
        | false -> Option.None

module String =
    let join separator (strings : string seq) = String.Join(separator, strings)

let formatTime = function
    | t when 0 <= t && t < 12 -> sprintf "%i AM" t
    | 12 -> "12 PM"
    | t when 13 <= t && t < 24 -> sprintf "%i PM" (t - 12)
    | _ -> "?"

let formatTimes =
    fun start end' -> sprintf "%s to %s" (formatTime start) (formatTime end')