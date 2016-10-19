CREATE TABLE [dbo].[QueueLengths]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY,
	[from_number] VARCHAR(12) NOT NULL,
	[location_id] INT NOT NULL,
	[queue_length] INT NOT NULL, 
    [as_of] DATETIME NOT NULL
)
