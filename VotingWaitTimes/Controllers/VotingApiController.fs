namespace VotingWaitTimes.Controllers

open System.Web.Configuration
open System.Web.Http

type VotingApiController() =
    inherit ApiController()

    member this.ConnectionString = WebConfigurationManager.AppSettings.["VOTING_CONN_STR"]