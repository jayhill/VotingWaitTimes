CREATE TABLE [dbo].[LocationPhoneNumbers]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY,
	[location_id] INT NOT NULL,
	[phone_number] VARCHAR(12) NOT NULL
)
