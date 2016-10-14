CREATE TABLE [dbo].[LocationSchedules]
(
	[location_id] INT NOT NULL,
	[date] DateTime NOT NULL,
	[start_time] INT NOT NULL,
	[end_time] INT NOT NULL, 
    CONSTRAINT [PK_LocationSchedule] PRIMARY KEY ([location_id], [date])
)
