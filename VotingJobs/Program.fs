open System
open Topshelf.FSharpApi
open VotingJobs

[<EntryPoint>]
let main _ = 

    let votingJob = VotingJob()
    let start _ = votingJob.Start(); true
    let stop _ = votingJob.Stop(); true
    
    Service.Default
    |> with_start start
    |> with_recovery (ServiceRecovery.Default |> restart (TimeSpan.FromMinutes 1.))
    |> with_stop stop
    |> description "Periodic jobs in support of Buncombe County voting queues SMS service"
    |> display_name "Voting Jobs"
    |> service_name "VotingJobs"
    |> run