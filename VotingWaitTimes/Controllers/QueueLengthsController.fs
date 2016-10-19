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

type QueueLengthsController() =
    inherit VotingApiController()

    let formatTime (dt : DateTime option) =
        match dt with
        | None -> " - "
        | Some time ->
            let hour = 
                match time.Hour with
                | h when 12 < h -> h - 13
                | h -> h
            let minutes = time.Minute.ToString().PadLeft(2, '0')
            sprintf "%i:%s" hour minutes

    let formatQueueLength (i : int option) =
        match i with
        | None -> " - "
        | Some mins -> mins.ToString()

    member this.Get () =
        let msg = 
            Data.getQueueLengths base.ConnectionString
            |> List.map (fun x ->
                sprintf "%s%s%s%s%s"
                    (x.id.ToString().PadRight(4))
                    (x.name.PadRight(48))
                    ((formatQueueLength x.queue_length).PadRight(8))
                    ((formatTime x.as_of).PadRight(12))
                    (defaultArg x.from_number " - "))
            |> fun xs -> sprintf "ID  %s%s%sREPORTED BY" ("LOCATION".PadRight(48)) ("QUEUE".PadRight(8)) ("AS OF".PadRight(12)) :: xs
            |> String.join "\n\n"
        
        let response = new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent(msg))
        base.ResponseMessage response :> IHttpActionResult