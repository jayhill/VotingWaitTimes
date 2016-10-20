CREATE TABLE [dbo].[Locations]
(
	[id] INT NOT NULL PRIMARY KEY,
	[name] VARCHAR(100) NOT NULL,
	[short_name] VARCHAR(20) NOT NULL,
	[street] VARCHAR(64) NOT NULL,
	[city_state_zip] VARCHAR(64) NOT NULL, 
    [code] CHAR(4) NOT NULL
)
