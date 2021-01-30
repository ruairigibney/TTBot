CREATE TEMP TABLE temp.Guild (Id ulong);
INSERT INTO temp.Guild (Id)
VALUES (***GUILD_ID***);

INSERT INTO EventAliasMapping (EventId, Alias)
VALUES ((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'ACC Pro'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Pro Am'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Pro/Am'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'GT Pro'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Mon Pro'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Monday Pro'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Acc Beg'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Beg'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'GT Beg'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Mon Beg'),
((SELECT Id FROM Event WHERE Shortname = 'ACC GT3 7.30pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Monday Beg'),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'iRacing'),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'MX 5'),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'MX-5'),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Sun'),
((SELECT Id FROM Event WHERE Shortname = 'iRacing 8.15pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Sunday'),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'FR 35'),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'FR 3.5'),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'PC 2'),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'PCars 2'),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Fri'),
((SELECT Id FROM Event WHERE Shortname = 'PCARS2 FR3.5 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Friday'),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'R3E'),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'RaceRoom'),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'TT'),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Wed'),
((SELECT Id FROM Event WHERE Shortname = 'RaceRoom Wed 8pm' AND GuildId = (SELECT Id FROM temp.Guild)), 'Wednesday');
