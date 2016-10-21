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
        | Some time -> time.ToShortTimeString()

    let formatQueueLength (i : int option) =
        match i with
        | None -> " - "
        | Some mins -> mins.ToString()

    let formatPhoneNumber (num : string option) =
        match num with
        | None -> "--------------"
        | Some num -> sprintf "(%s) XXX-%s" (num.[2..4]) (num.[8..])

    member this.Get () =
        let msg = 
            Data.getQueueLengths base.ConnectionString
            |> List.map (fun x ->
                sprintf "%s  %s%s   %s     %s"
                    (x.id.ToString().PadLeft(2))
                    (x.name.PadRight(48))
                    ((formatQueueLength x.queue_length).PadLeft(4))
                    ((formatTime x.as_of).PadLeft(12))
                    (formatPhoneNumber x.from_number))
            |> fun xs -> sprintf "ID  %s%s%s     REPORTED BY" ("LOCATION".PadRight(44)) ("QUEUE".PadLeft(8)) ("AS OF".PadLeft(15)) :: xs
            |> String.join "\n\n"
        
        let response = new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent(msg))
        base.ResponseMessage response :> IHttpActionResult