namespace VotingWaitTimes.Controllers

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Web
open System.Web.Http
open System.Xml
open System.Xml.Linq
open Newtonsoft.Json
open FSharp.Data.SqlClient

type location = {
    name : string
    street : string
    cityStateZip : string
    date : DateTime
    hours : string
}

type EarlyVotingLocationsController() =
    inherit ApiController()

    let connStr = System.Web.Configuration.WebConfigurationManager.AppSettings.["VOTING_CONN_STR"]

    member x.Get() =
        let schedules =
            Data.getLocationSchedules connStr
            |> Seq.filter (fun x -> x.date > DateTime.Today || x.end_time > DateTime.Now.Hour)
            |> List.ofSeq

        let results =
            match schedules with
            | [] -> List.empty
            | _ ->
                let date = (schedules |> Seq.minBy (fun x -> x.date)).date

                schedules
                |> List.filter (fun x -> x.date = date)
                |> List.map (fun x ->
                    {
                        name = x.name
                        street = x.street
                        cityStateZip = x.city_state_zip
                        date = x.date
                        hours = formatTimes x.start_time x.end_time
                    })

        base.Json results