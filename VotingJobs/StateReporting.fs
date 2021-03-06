﻿module StateReporting

open System
open System.Configuration
open System.IO
open System.Net.Mail
open FSharp.Data

let [<Literal>] private designConnStr = @"Data Source=localhost\DEVSQL;Initial Catalog=VotingWaitTimes;Integrated Security=True"
let connStr = Common.connStr
let ``59 minutes`` = TimeSpan.FromMinutes(59.)

type private StateReportingQuery =
    SqlCommandProvider<
        "SELECT l.code, wt.queue_length, wt.as_of, xyz.start_time, xyz.end_time
        FROM locations as l left outer join
            (select * from QueueLengths as wtx
                WHERE CONVERT(DATE, as_of) = CONVERT(DATE, GETDATE())
                AND as_of = (SELECT MAX(as_of) FROM QueueLengths WHERE location_id = wtx.location_id)) as wt ON wt.location_id = l.id
            LEFT OUTER JOIN (SELECT * FROM LocationSchedules AS ls WHERE ls.date = CONVERT(DATE, GETDATE())) xyz
                ON xyz.location_id = l.id
        WHERE start_time IS NOT NULL AND end_time IS NOT NULL", designConnStr>

type private LastReport = SqlCommandProvider<"SELECT TOP 1 report_time FROM StateReporting ORDER BY id DESC", designConnStr, SingleRow = true>
type private UpdateLastReport = SqlCommandProvider<"INSERT INTO StateReporting (report_time) VALUES (GETDATE())", designConnStr>

let private pad (i: int) = i.ToString().PadLeft(2, '0')
let private setting key = Common.appSettingsReader.GetValue(key, typeof<string>) :?> string

let private sendEmailToState data (now : DateTime) =
    match data with
    | [] -> appLog <| Msg "no data to send"
    | _ ->
        let emailHost    = setting "emailHost"
        let fromEmail    = setting "emailFrom"
        let toEmail      = setting "emailTo"
        let bccEmail     = setting "emailBcc"
        let replyToEmail = setting "emailReplyTo"
        let pwEmail      = setting "emailPw"

        let body = data |> List.fold (fun state line -> sprintf "%s\n%s" state line) ""
        let email = new MailMessage(fromEmail, toEmail, "Buncombe County voter queues report", body)
        bccEmail.Split([|';'|], StringSplitOptions.None) |> Array.iter email.Bcc.Add
        email.ReplyToList.Add(replyToEmail)

        use client = new SmtpClient(emailHost)
        client.Credentials <- System.Net.NetworkCredential(fromEmail, pwEmail)

        appLog <| Msg ("sending email at " + (now.ToShortDateString()))
        try
            client.Send(email)
        with
            | ex -> appLog <| Error (ex.ToString())

let reportToState () =
    let now = DateTime.Now
    appLog <| Msg (sprintf "began running state reporting job at %s" (now.ToShortTimeString()))
    StateReportingQuery.Create(connStr).Execute()
        |> Seq.filter (fun x -> TimeSpan.FromHours(float x.start_time.Value).Add(TimeSpan.FromMinutes(-1.)) <= now.TimeOfDay)
        |> Seq.filter (fun x -> now.TimeOfDay <= TimeSpan.FromHours(float x.end_time.Value).Add(TimeSpan.FromMinutes(15.)))
        |> Seq.filter (fun x -> x.queue_length.IsSome)
        |> Seq.map (fun x -> sprintf "Buncombe,%s,%s" x.code (x.queue_length.Value.ToString()))
        |> List.ofSeq
        |> function
            | [] -> appLog <| Msg ("no candidates for reporting at this time" + (now.ToShortTimeString()))
            | data ->
                let exec () =
                    sendEmailToState data now
                    UpdateLastReport.Create(connStr).Execute() |> ignore
                match LastReport.Create(connStr).Execute() with
                    | None -> exec ()
                    | Some lastReport when lastReport.Date < now.Date -> exec ()
                    | Some lastReport when ``59 minutes`` < (now.TimeOfDay - lastReport.TimeOfDay) -> exec ()
                    | _ -> ()