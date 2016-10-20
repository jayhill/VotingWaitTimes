namespace VotingJobs

open System
open System.Configuration
open System.IO
open System.Net.Mail
open System.Timers
open FSharp.Data

type VotingJob () =
    let interval = float (1000 * 60 * 2)
    let timer = new Timer(interval)
    do timer.AutoReset <- true

    let doJobs () = StateReporting.reportToState()

    do timer.Elapsed.Add (fun _ ->
        Console.WriteLine "timer elapsed"
        try
            doJobs ()
        with
            | ex -> Console.WriteLine ex.Message)

    member this.Start () =
        doJobs ()
        timer.Start()
        Console.WriteLine "timer started"
    member this.Stop () =
        Console.WriteLine "timer stopped"
        timer.Stop()