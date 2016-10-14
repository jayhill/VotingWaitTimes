CREATE TABLE [dbo].[MessageLog]
(
	[id] INT NOT NULL PRIMARY KEY IDENTITY,
	[direction] VARCHAR(8) NOT NULL,
	[phone_number] VARCHAR(12) NULL,
	[body] VARCHAR(MAX) NULL, 
    [status] VARCHAR(12) NULL, 
    [timestamp] DATETIME NOT NULL
)
