namespace VotingWaitTimes.Controllers

open System
open System.Collections.Generic
open System.Web.Http
open System.Net
open System.Net.Http
open System.Net.Http.Formatting

type Attributes = {
    ``to`` : string
    from : string
}

type SmsController () =
    inherit VotingApiController()

    let log = Data.logOutgoingMessage base.ConnectionString

    member private this.CreateXmlResponse (xml : string) =
        let response = new HttpResponseMessage(HttpStatusCode.OK)
        let content = new StringContent(xml, Text.Encoding.UTF8, "application/xml")
        response.Content <- content
        base.ResponseMessage response :> IHttpActionResult

    member this.Post (msg : Twilio.Message) =
        match SmsHandler.handleMessage msg with
        | Some "" -> this.CreateXmlResponse <| Twilio.TwiML.TwilioResponse().ToString()
        | Some result ->
            let attributes = { from = "+18553438683"; ``to`` = msg.From }
            let message = Twilio.TwiML.TwilioResponse().Sms(result, attributes)
            log (Data.PhoneNumber msg.From) result
            this.CreateXmlResponse (message.ToString())
        | _ -> base.BadRequest (sprintf "Possible invalid SID: %s" msg.AccountSid) :> IHttpActionResult