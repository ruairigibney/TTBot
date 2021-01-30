CREATE TEMP TABLE temp.Guild (Id ulong);
INSERT INTO temp.Guild (Id)
VALUES (***GUILD_ID***);

INSERT INTO ExcelSheetEventMapping (EventId, Sheetname, IsRoundsSheet)
VALUES ((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'ACC ProAmChampionship', false),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'ACC Beg Championship', false),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'iRacing MX5 Championship', false),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'FR3.5 Championship', false),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'AudiTT Championship', false);

INSERT INTO ExcelSheetEventMapping (EventId, Sheetname, IsRoundsSheet)
VALUES ((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'ACC ProAm Rounds', true),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'ACC Beg Rounds', true),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'iRacing MX5 Rounds', true),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'FR3.5 Rounds', true),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'AudiTT Rounds', true);

DROP TABLE temp.Guild;