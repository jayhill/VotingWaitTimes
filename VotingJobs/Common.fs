[<AutoOpen>]
module Common

open System
open System.Diagnostics

let appSettingsReader = Configuration.AppSettingsReader()
let connStr = appSettingsReader.GetValue("connStr", typeof<string>) :?> string

let [<Literal>] eventSource = "VotingJobs"

type Log =
    | Msg of string
    | Error of string

let appLog msg =
    try
        match EventLog.SourceExists(eventSource) with
            | true -> ()
            | false -> EventLog.CreateEventSource(eventSource, "Application")
        match msg with
            | Msg s ->
                Console.WriteLine s
                match appSettingsReader.GetValue("logInfo", typeof<string>) :?> string with
                | "true" -> EventLog.WriteEntry(eventSource, s, EventLogEntryType.Information)
                | _ -> ()
            | Error s ->
                Console.ForegroundColor <- ConsoleColor.Red
                Console.WriteLine(s)
                Console.ForegroundColor <- ConsoleColor.White
                EventLog.WriteEntry(eventSource, s, EventLogEntryType.Error)
    with
        | ex -> Console.WriteLine ("failed to write log message\n" + (ex.ToString()))