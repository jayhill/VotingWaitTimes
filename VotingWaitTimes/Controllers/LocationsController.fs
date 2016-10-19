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
    inherit VotingApiController()

    let formatQueueLength startTime endTime (now : DateTime) queueLength (asOf : DateTime option) =
        match queueLength with
        | None -> ""
        | Some mins ->
            match now.Hour with
            | h when h < startTime || endTime < h -> ""
            | _ ->
                match asOf with
                | Some asOf -> sprintf "%i people (as of %s)" mins (asOf.ToShortTimeString())
                | None -> sprintf "%i people" mins

    member x.Get() =
        let now = DateTime.Now
        let schedules =
            Data.getLocationSchedulesWithCurrentWaitTimes base.ConnectionString
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
                        wait = formatQueueLength x.start_time x.end_time now x.queue_length x.as_of
                    })

        base.Json results