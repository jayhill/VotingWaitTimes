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
    wait : string
}

type LocationsController() =
    inherit ApiController()

    let connStr = System.Web.Configuration.WebConfigurationManager.AppSettings.["VOTING_CONN_STR"]

    let getWaitMinutes startTime endTime (now : DateTime) waitMins (asOf : DateTime option) =
        match waitMins with
        | None -> ""
        | Some mins ->
            match now.Hour with
            | h when h < startTime || endTime < h -> ""
            | _ ->
                match asOf with
                | Some asOf -> sprintf "%i minutes (as of %s)" mins (asOf.ToShortTimeString())
                | None -> sprintf "%i minutes" mins

    member x.Get() =
        let now = DateTime.Now
        let schedules =
            Data.getLocationSchedulesWithCurrentWaitTimes connStr
            |> Seq.filter (fun x -> x.location_id > 0)
            |> Seq.filter (fun x -> now.Date < x.schedule_date || now.Hour < x.end_time)
            |> List.ofSeq

        let results =
            match schedules with
            | [] -> List.empty
            | _ ->
                let date = (schedules |> Seq.minBy (fun x -> x.schedule_date)).schedule_date

                schedules
                |> List.filter (fun x -> x.schedule_date = date)
                |> List.map (fun x ->
                    {
                        name = x.location_name
                        street = x.street
                        cityStateZip = x.city_state_zip
                        date = x.schedule_date
                        hours = formatTimes x.start_time x.end_time
                        wait = getWaitMinutes x.start_time x.end_time now x.wait_minutes x.as_of
                    })

        base.Json results