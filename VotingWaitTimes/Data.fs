module Data

open System
open FSharp.Data

type LocationName = LocationName of string
type PhoneNumber = PhoneNumber of string
    with
        override this.ToString() = match this with PhoneNumber s -> s
        static member toString (pn : PhoneNumber) = pn.ToString()

type PhoneNumberStatus =
    | Added
    | Registered
    | Unregistered

/// Design-time connection string used by the SQL Client Type Provider.
/// A sample database must be available to build the project.
/// It can be created by publishing the Data project and a different
/// connection string can be passed in to the SqlCommandProvider
/// and SqlProgrammabilityProvider types at runtime.
let [<Literal>] private connectionString = @"Data Source=localhost\DEVSQL;Initial Catalog=VotingWaitTimes;Integrated Security=True"

type private LogMessage = 
    SqlCommandProvider<"INSERT INTO MessageLog (direction, phone_number, body, status, timestamp) VALUES (@direction, @phoneNumber, @body, @status, GETDATE())", connectionString>
let private logMessage (connStr : string) direction (phoneNumber : PhoneNumber) body status = 
    let provider = LogMessage.Create connStr
    provider.Execute (direction, phoneNumber.ToString(), body, status) |> ignore
let logOutgoingMessage (connStr : string) (sentTo : PhoneNumber) body  =
    logMessage connStr "SENT" sentTo body String.Empty
let logIncomingMessage (connStr : string) (msg : Twilio.Message) =
    logMessage connStr "RECEIVED" (PhoneNumber msg.From) msg.Body msg.Status

type private GetLocationSchedules =
    SqlCommandProvider<
        "SELECT l.*, ls.*
            FROM LocationSchedules AS ls JOIN Locations as l ON ls.[location_id] = l.[id]
            WHERE l.[id] > 0 AND ls.[date] >= CONVERT(DATE, GETDATE())", connectionString>
let getLocationSchedules (connectionString : string) =
    GetLocationSchedules.Create(connectionString).Execute()

type private GetLocationSchedulesWithCurrentWaitTimes =
    SqlCommandProvider<
        "SELECT lx.id as location_id, lx.name as location_name, lx.short_name, lx.street, lx.city_state_zip,
        		lsx.[date] AS schedule_date, lsx.start_time, lsx.end_time,
        		wtxxx.queue_length, wtxxx.as_of FROM LocationSchedules AS lsx
        	JOIN Locations AS lx ON lx.id = lsx.location_id
        	LEFT OUTER JOIN
        		(SELECT wtxx.* FROM QueueLengths as wtxx
        			WHERE wtxx.as_of = (SELECT MAX(as_of) FROM QueueLengths as wtx WHERE wtx.location_id = wtxx.location_id
        									AND CONVERT(DATE, wtx.as_of) = CONVERT(DATE, wtxx.as_of))) AS wtxxx
        	ON lsx.location_id = wtxxx.location_id AND lsx.[date] = CONVERT(DATE, wtxxx.as_of)" , connectionString>
let getLocationSchedulesWithCurrentWaitTimes (connectionString : string) =
    (GetLocationSchedulesWithCurrentWaitTimes.Create connectionString).Execute() |> List.ofSeq

type private GetLocationById =
    SqlCommandProvider<"SELECT * FROM Locations WHERE id = @id", connectionString>
let getLocationById (connectionString : string) = 
    let provider = GetLocationById.Create(connectionString)
    provider.Execute >> Seq.tryHead
    
type private GetScheduleForLocation =
    SqlCommandProvider<"SELECT * FROM LocationSchedules WHERE location_id = @id", connectionString>
let getScheduleForLocation (connectionString : string) =
    let provider = GetScheduleForLocation.Create(connectionString)
    provider.Execute >> List.ofSeq

type private GetLocationByPhoneNumber =
    SqlCommandProvider<
        "SELECT TOP 1 l.* FROM LocationPhoneNumbers as lpn 
        JOIN Locations as l ON l.id = lpn.location_id
        WHERE lpn.phone_number = @phone", connectionString>
let getLocationByPhoneNumber (connStr : string) = 
    let provider = GetLocationByPhoneNumber.Create connStr
    fun phoneNumber -> phoneNumber.ToString() |> provider.Execute |> Seq.tryHead

type private UpdateQueueLength =
    SqlCommandProvider<"INSERT INTO QueueLengths VALUES (@phoneNumber, @locationId, @queueLength, GETDATE())", connectionString>
let updateQueueLength (connStr : string) (sender : PhoneNumber) locationId queueLength =
    UpdateQueueLength.Create(connStr).Execute(sender.ToString(), locationId, queueLength)

type private AddPhoneNumber =
    SqlCommandProvider<"INSERT INTO PhoneNumbers VALUES (@phoneNumber)", connectionString>

type private RegisterPhoneNumber =
    SqlCommandProvider<"INSERT INTO LocationPhoneNumbers (phone_number, location_id) VALUES (@phoneNUmber, @locationId)", connectionString>

type private UpdateRegisteredPhoneNumber =
    SqlCommandProvider<"UPDATE LocationPhoneNumbers SET location_id = @locationId WHERE phone_number = @phoneNumber", connectionString>
let private updateRegisteredPhoneNumber (connStr : string) =
    let provider = UpdateRegisteredPhoneNumber.Create connStr
    fun locationId (phoneNumber : PhoneNumber) ->
        provider.Execute(locationId, phoneNumber.ToString()) |> ignore

type private PhoneNumberIsRegistered =
    SqlCommandProvider<"SELECT COUNT(*) FROM LocationPhoneNumbers WHERE phone_number = @phoneNumber", connectionString, SingleRow = true>
let phoneNumberIsRegistered (connStr : string)  =
    let provider = PhoneNumberIsRegistered.Create connStr
    fun (phoneNumber : PhoneNumber) ->
        match phoneNumber.ToString() |> provider.Execute with
        | Some (Some i) when i > 0 -> true
        | _ -> false

let registerPhoneNumber (connStr : string) =
    let provider = RegisterPhoneNumber.Create connStr
    fun (phoneNumber : PhoneNumber) locationId ->
        match phoneNumberIsRegistered connStr phoneNumber with
        | true -> updateRegisteredPhoneNumber connStr locationId phoneNumber
        | false -> provider.Execute (phoneNumber.ToString(), locationId) |> ignore

type private GetPhoneNumberStatus =
    SqlCommandProvider<"SELECT TOP 1 * FROM PhoneNumbers WHERE phone_number = @phoneNumber", connectionString, SingleRow = true>
let getPhoneNumberStatus (connStr : string) =
    let addPhoneNumber = AddPhoneNumber.Create connStr
    let provider = GetPhoneNumberStatus.Create connStr
    fun (phoneNumber : PhoneNumber) ->
        phoneNumber.ToString()
        |> provider.Execute
        |> function
            | None ->
                phoneNumber.ToString() |> addPhoneNumber.Execute |> ignore
                PhoneNumberStatus.Added
            | Some _ ->
                match phoneNumberIsRegistered connStr phoneNumber with
                | true -> PhoneNumberStatus.Registered
                | false -> PhoneNumberStatus.Unregistered
                    
type private GetQueueLengths =
    SqlCommandProvider<"SELECT l.id, l.name, wt.queue_length, wt.from_number, wt.as_of 
FROM locations as l left outer join
	(select * from QueueLengths as wtx
		WHERE CONVERT(DATE, as_of) = CONVERT(DATE, GETDATE())
		AND as_of = (SELECT MAX(as_of) FROM QueueLengths WHERE location_id = wtx.location_id)) as wt ON wt.location_id = l.id", connectionString>

let getQueueLengths (connStr : string) = GetQueueLengths.Create(connStr).Execute() |> List.ofSeq