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
            _client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true });
        }

        private async Task MainAsync(string[] args)
        {
            var services = new ServiceCollection();
            CreateDataDirectory();
            InitServices(services, args);
            InitDapperTypeHandlers();
            _serviceProvider = services.BuildServiceProvider();
            ScaffoldDatabase();
            await InitCommands();

            _client.MessageReceived += MessageReceived;
            _client.Log += Log;
            await _client.LoginAsync(TokenType.Bot, _configuration.GetValue<string>("Token"));
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private static string GetDataDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TTBot");

        private void CreateDataDirectory()
        {
            var path = GetDataDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string GetConnString() => $"{Path.Combine(GetDataDirectory(), "database.sqlite")}";

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
            }
        }
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
