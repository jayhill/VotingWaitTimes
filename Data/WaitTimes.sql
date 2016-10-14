CREATE TABLE [dbo].[WaitTimes]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY,
	[from_number] VARCHAR(12) NOT NULL,
	[location_id] INT NOT NULL,
	[wait_minutes] INT NOT NULL, 
    [as_of] DATETIME NOT NULL
)
