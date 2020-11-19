using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using TTBot.DataAccess;
using Dapper;
using TTBot.DataAccess.Dapper;
using TTBot.Services;
using Microsoft.Data.Sqlite;
using ServiceStack.OrmLite;
using TTBot.Models;
using ServiceStack.Data;
using System.Collections.Generic;

namespace TTBot
{
    class Program
    {
        private CommandService _commandService;
        private DiscordSocketClient _client;
        private ServiceProvider _serviceProvider;
        private IConfiguration _configuration;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Bot..");
            new Program().MainAsync(args).GetAwaiter().GetResult();
        }

        public Program()
        {
            _commandService = new CommandService();
            _client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true, MessageCacheSize = 1000, GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.Guilds | GatewayIntents.GuildMessageReactions });
        }

        private async Task MainAsync(string[] args)
        {
            var services = new ServiceCollection();

            InitServices(services, args);
            CreateDataDirectory();
            InitDapperTypeHandlers();
            _serviceProvider = services.BuildServiceProvider();
            ScaffoldDatabase();
            await InitCommands();

            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += OnReactionAdd;
            _client.ReactionRemoved += OnReactionRemove;

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _configuration.GetValue<string>("Token"));
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task OnReactionRemove(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var eventSignups = _serviceProvider.GetRequiredService<IEventSignups>();
            var events = _serviceProvider.GetRequiredService<IEvents>();
            var eventParticipantSets = _serviceProvider.GetRequiredService<IEventParticipantService>();
            var message = await cacheableMessage.GetOrDownloadAsync();

            if (!reaction.User.IsSpecified)
                return;

            if (message.Author.Id != _client.CurrentUser.Id)
                return;

            var @event = await events.GetEventByMessageIdAsync(cacheableMessage.Id);
            if (@event == null || @event.Closed)
            {
                return;
            }

            var existingSignup = await eventSignups.GetSignupAsync(@event, reaction.User.Value);
            if (existingSignup == null)
            {
                return;
            }

            await eventSignups.Delete(existingSignup);
            await eventParticipantSets.UpdatePinnedMessageForEvent(channel, @event, message);
            await reaction.User.Value.SendMessageAsync($"Thanks! You've been removed from {@event.Name}.");

        }
        private async Task OnReactionAdd(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var eventSignups = _serviceProvider.GetRequiredService<IEventSignups>();
            var events = _serviceProvider.GetRequiredService<IEvents>();
            var eventParticipantSets = _serviceProvider.GetRequiredService<IEventParticipantService>();
            var message = await cacheableMessage.GetOrDownloadAsync();

            async Task CancelSignup(string reason)
            {
                await reaction.User.Value.SendMessageAsync(reason);
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                return;
            }

            if (!reaction.User.IsSpecified)
            {
                return;
            }

            if (message.Author.Id != _client.CurrentUser.Id)
                return;

            var @event = await events.GetEventByMessageIdAsync(cacheableMessage.Id);
            if (@event == null || @event.Closed)
            {
                return;
            }

            var existingSignup = await eventSignups.GetSignupAsync(@event, reaction.User.Value);

            if (existingSignup != null)
            {
                var noOfReactionsForUser = 0;
                foreach (var r in message.Reactions) //hack to handle events signed up to with command..
                {
                    var reactors = await message.GetReactionUsersAsync(r.Key, 999).FlattenAsync();
                    if (reactors.Any(r => r.Id == reaction.UserId))
                    {
                        noOfReactionsForUser++;
                    }
                }
                if (noOfReactionsForUser > 1)
                {
                    await CancelSignup($"You are already signed for this event {reaction.User.Value.Mention}");
                }

                return;
            }

            if (@event.SpaceLimited && @event.Full)
            {
                await CancelSignup($"Sorry, {reaction.User.Value.Mention} this event is currently full!");
                return;
            }

            await eventSignups.AddUserToEvent(@event, reaction.User.Value);
            await eventParticipantSets.UpdatePinnedMessageForEvent(channel, @event, message);
            await reaction.User.Value.SendMessageAsync($"Thanks! You've been signed up to {@event.Name}. If you can no longer attend just remove your reaction from the signup message!");
        }

        private async Task OnReactionChange(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction _)
        {
            var message = await cacheableMessage.GetOrDownloadAsync();
            if (message.Author.Id != _client.CurrentUser.Id)
                return;
            var confirmationPrinter = _serviceProvider.GetRequiredService<IConfirmationCheckPrinter>();
            var confirmationsDAL = _serviceProvider.GetRequiredService<IConfirmationChecks>();
            var eventsDal = _serviceProvider.GetRequiredService<IEvents>();
            var confirmationCheck = await confirmationsDAL.GetConfirmationCheckByMessageId(message.Id);
            if (confirmationCheck == null)
            {
                return;
            }

            var @event = await eventsDal.GetActiveEvent(confirmationCheck.EventId);
            await confirmationPrinter.WriteMessage(channel, message, @event);
        }

        private string GetDataDirectory()
        {
            return _configuration.GetValue<string>("Database", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TTBot"));

        }

        private void CreateDataDirectory()
        {
            var path = GetDataDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string GetConnString() => $"{Path.Combine(GetDataDirectory(), "database.sqlite")}";

        private void InitDapperTypeHandlers()
        {
            SqlMapper.RemoveTypeMap(typeof(TimeSpan));
            SqlMapper.AddTypeHandler(typeof(TimeSpan), new TimeSpanHandler());
        }

        private void ScaffoldDatabase()
        {
            var dbConFactory = _serviceProvider.GetRequiredService<IDbConnectionFactory>();
            using (var connection = dbConFactory.Open())
            {
                connection.CreateTableIfNotExists<Leaderboard>();
                connection.CreateTableIfNotExists<LeaderboardEntry>();
                connection.CreateTableIfNotExists<LeaderboardModerator>();
                connection.CreateTableIfNotExists<Event>();
                connection.CreateTableIfNotExists<EventSignup>();
                connection.CreateTableIfNotExists<ConfirmationCheck>();
                connection.Execute(@"CREATE VIEW IF NOT EXISTS EventsWithCount
                                    AS
                                    SELECT *, (select count(*) from EventSignup where EventId = event.Id) as ParticipantCount
                                    FROM [Event]
                                    ");

            }
        }

        private void InitServices(ServiceCollection services, string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables("TTBot_")
                .AddCommandLine(args);

            services.AddSingleton<IConfiguration>(this._configuration = builder.Build());
            services.AddScoped<IModerator, Moderator>();
            services.AddScoped<ILeaderboards, Leaderboards>();
            services.AddScoped<ILeaderboardEntries, LeaderboardEntries>();
            var conString = GetConnString();
            services.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(conString, SqliteDialect.Provider));
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IEvents, Events>();
            services.AddScoped<IEventSignups, EventSignups>();
            services.AddScoped<IConfirmationChecks, ConfirmationChecks>();
            services.AddScoped<IConfirmationCheckPrinter, ConfirmationCheckPrinter>();
            services.AddScoped<IEventParticipantService, EventParticipantService>();
            services.AddSingleton(_client);
        }

        private async Task InitCommands()
        {
            await _commandService.AddModulesAsync(typeof(Program).Assembly, _serviceProvider);
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            var message = socketMessage as SocketUserMessage;
            int argPos = 0;

            if (message == null || !message.HasCharPrefix('!', ref argPos) || message.Author.IsBot)
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);

            var commandResult = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

            if (!commandResult.IsSuccess)
            {
                Console.WriteLine("Error: " + commandResult.ErrorReason);
                Console.WriteLine(commandResult.ToString());

                if (commandResult.Error == CommandError.BadArgCount || commandResult.Error == CommandError.ParseFailed)
                {
                    await socketMessage.Channel.SendMessageAsync("Error running command. Try wrapping command parameters in \"quotes\"");
                }
            }
        }
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
