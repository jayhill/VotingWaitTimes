module StateReporting

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

let private writeFile (data : string list) (now : DateTime) =
    match data with
    | [] ->
        appLog <| Msg ("found no data to write to file at " + now.ToShortTimeString())
        None
    | data ->
        let filename = sprintf "buncombe_report_%i%s%s_%s%s.csv" now.Year (pad now.Month) (pad now.Day) (pad now.Hour) (pad now.Minute)
        appLog <| Msg (sprintf "writing file [%s] at %s" filename (now.ToShortTimeString()))
        use file = File.CreateText(filename)
        data |> List.iter file.WriteLine
        Some filename

let private sendEmailToState filename (now : DateTime) =
    let emailHost    = setting "emailHost"
    let fromEmail    = setting "emailFrom"
    let toEmail      = setting "emailTo"
    let bccEmail     = setting "emailBcc"
    let replyToEmail = setting "emailReplyTo"
    let pwEmail      = setting "emailPw"

    let email =
        new MailMessage(fromEmail, toEmail, "Buncombe County voter queues report",
            sprintf "Attached are the reported voter queue lengths for Buncombe County as of %s." (now.ToShortTimeString()))
    bccEmail.Split([|';'|], StringSplitOptions.None) |> Array.iter email.Bcc.Add

    let attachment = new Attachment(filename)
    email.Attachments.Add(attachment)
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
        |> Seq.filter (fun x -> TimeSpan.FromHours(float x.start_time.Value).Add(TimeSpan.FromMinutes(-15.)) <= now.TimeOfDay)
        |> Seq.filter (fun x -> now.TimeOfDay <= TimeSpan.FromHours(float x.end_time.Value).Add(TimeSpan.FromMinutes(15.)))
        |> Seq.filter (fun x -> x.queue_length.IsSome)
        |> Seq.map (fun x -> sprintf "Buncombe,%s,%s" x.code (x.queue_length.Value.ToString()))
        |> List.ofSeq
        |> function
            | [] ->
                appLog <| Msg ("found no entries for reporting at " + (now.ToShortTimeString()))
            | data ->
                let exec =
                    match writeFile data now with
                    | None -> ()
                    | Some filename ->
                        sendEmailToState filename now
                        UpdateLastReport.Create(connStr).Execute() |> ignore
                match LastReport.Create(connStr).Execute() with
                | None -> exec
                | Some lastReport when ``59 minutes`` < now.TimeOfDay - lastReport.TimeOfDay -> exec
                | _ -> ()