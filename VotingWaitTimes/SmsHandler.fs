module SmsHandler

open System
open System.Text.RegularExpressions
open Data

type ConfigMgr = System.Web.Configuration.WebConfigurationManager

let connStr = System.Web.Configuration.WebConfigurationManager.AppSettings.["VOTING_CONN_STR"]
let accountSid = ConfigMgr.AppSettings.["TWILIO_ACCT_SID"]

let private listLocations () =
    Data.getLocationSchedules connStr
    |> Seq.distinctBy (fun x -> x.id)
    |> Seq.map (fun x -> sprintf "%i) %s" x.id x.short_name)
    |> String.join "\n"

let private updateQueue sender queueLength =
    match Data.getLocationByPhoneNumber connStr sender with
    | None -> "This phone is not registered to a location.\nText LIST to see a list of early voting locations."
    | Some loc ->
        Data.updateQueueLength connStr sender loc.id queueLength |> ignore
        sprintf "Queue length for %s is now %i people.\nResend to make a correction." loc.name queueLength

let private selectLocation sender number =
    match number with
    | ParseInt i ->
        match Data.getLocationById connStr i with
        | Some loc ->
            Data.registerPhoneNumber connStr sender loc.id
            let now = DateTime.Now
            let nextDay =
                Data.getScheduleForLocation connStr i
                |> List.filter (fun x -> now.Date < x.date || (x.date = now.Date && x.end_time < now.Hour))
                |> List.tryHead
            match nextDay with
                | None -> sprintf "%s is not scheduled for any more early voting days." loc.name
                | Some x ->
                    let details =
                        match x.date = now.Date, x.start_time < now.Hour, now.Hour < x.end_time with
                        | true, true, true -> sprintf "open until %s today." (formatTime x.end_time)
                        | true, false, _ -> sprintf "open from %s today." (formatTimes x.start_time x.end_time)
                        | _ -> sprintf "next open on %s from %s." (x.date.ToShortDateString()) (formatTimes x.start_time x.end_time)
                    sprintf "Successfully registered phone to %s, %s" loc.name details
        | _ ->
            sprintf "No location found for number [%i]. Please choose again.\nText LIST to see locations." i
    | _ -> sprintf "Failed to register a location with input [%s].\nText OPT for options." number

let private options = "Early Voting Wait Queues
LIST : Show locations
LOC # : Register phone to loc
# : Update queue length"

let private unhandled sender status =
    match status with
    | PhoneNumberStatus.Registered -> options
    | PhoneNumberStatus.Unregistered ->
        "You must register this phone to an early voting location.\nText LIST to see all locations."
    | PhoneNumberStatus.Added -> 
        sprintf "Welcome to Buncombe County early voting wait queues.\nText LIST to see early voting locations.\nText LOC # to choose your location."

let private handleValidMsg (msg : Twilio.Message) =
    let body = msg.Body.Trim()
    let sender = PhoneNumber msg.From
    let status = Data.getPhoneNumberStatus connStr sender
    match body with
    | "" as blank -> blank
    | IgnoreCase "list"
    | RegexMatch "^loc(?:ation)?$" _ -> listLocations ()
    | RegexCapture "^loc(?:ation)?\s+(\d+)" matches -> selectLocation sender (matches.[1])
    | ParseInt i ->
        match i with
        | _ when i < 0 -> "ERROR: [%i] is not valid.\nThe number of voters in queue must be non-negative."
        | _ -> updateQueue sender i
    | RegexMatch "^opt(?:ion)?s?" _ -> options
    | _ -> unhandled sender status

let handleMessage (msg : Twilio.Message) =
    Data.logIncomingMessage connStr msg
    match msg.AccountSid with
        | sid when sid = accountSid -> handleValidMsg msg |> Some
        | _ -> None