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
    inherit ApiController()

    member x.Post (msg : Twilio.Message) =
        match SmsHandler.handleMessage msg with
        | Some result ->
            let attributes = { from = "+18553438683"; ``to`` = msg.From }
            let message = Twilio.TwiML.TwilioResponse().Sms(result, attributes)

            let response = new HttpResponseMessage(HttpStatusCode.OK)
            let content = new StringContent(message.ToString(), Text.Encoding.UTF8, "application/xml")
            response.Content <- content
            base.ResponseMessage response :> IHttpActionResult
        | _ -> base.BadRequest (sprintf "Possible invalid SID: %s" msg.AccountSid) :> IHttpActionResult