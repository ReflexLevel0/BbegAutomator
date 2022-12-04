using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace BbegAutomator
{
	public class Program
	{
		//TODO: crash the program if an exception occurs 
		private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

		private IHost _host;
		private static readonly DiscordSocketClient Client = new();
		private static Config _config;
		private const string UpdateCommandName = "update";
		private const string CreateEventCommandName = "event-create";
		private const string ListLeaderboardCommandName = "leaderboard-list";
		private const string ListAllLeaderboardsCommandName = "leaderboard-list-all";

		private async Task MainAsync()
		{
			_config = await Config.GetConfigAsync();

			//Setting up dependency injection
			_host = Host
				.CreateDefaultBuilder()
				.ConfigureServices((_, services) =>
					services
						.AddSingleton(Client)
						.AddSingleton<IDiscordClient>(Client)
						.AddSingleton(_config)).Build();

			Client.Log += Log;
			
			Client.SlashCommandExecuted += SlashCommandHandlerAsync;
			Client.Ready += InitCommandsAsync;

			await Client.LoginAsync(TokenType.Bot, _config.BotToken);
			await Client.StartAsync();

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		public static async Task Log(LogMessage msg)
		{
			if (Client.LoginState == LoginState.LoggedIn)
			{
				foreach (ulong id in _config.LoggingIds)
				{
					var user = await Client.GetUserAsync(id);
					await user.SendMessageAsync(msg.Message);
				}
			}

			Console.WriteLine(msg.ToString());
		}

		private async Task InitCommandsAsync()
		{
			{
				//Setting up app commands
				var guild = Client.GetGuild(_config.GuildId);
				var updateCommand = new SlashCommandBuilder();
				updateCommand.WithName(UpdateCommandName);
				updateCommand.WithDescription("Updates the bbeg leaderboard for the current event");
				var createEventCommand = new SlashCommandBuilder();
				createEventCommand.WithName(CreateEventCommandName);
				createEventCommand.WithDescription("Creates a new event (all updates will update the leaderboard for this event)");
				createEventCommand.AddOption("name", ApplicationCommandOptionType.String, "Name of the event that will be created", isRequired: true);
				var listLeaderboardEventCommand = new SlashCommandBuilder();
				listLeaderboardEventCommand.WithName(ListLeaderboardCommandName);
				listLeaderboardEventCommand.WithDescription("Prints out a leaderboard for the specified event");
				listLeaderboardEventCommand.AddOption("event-name", ApplicationCommandOptionType.String, "Name of the event to be printer out", isRequired: true);
				try
				{
					await guild.CreateApplicationCommandAsync(updateCommand.Build());
					await guild.CreateApplicationCommandAsync(createEventCommand.Build());
					await guild.CreateApplicationCommandAsync(listLeaderboardEventCommand.Build());
				}
				catch (ApplicationCommandException exception)
				{
					Console.WriteLine(exception.Message);
				}
			}
		}

		private async Task SlashCommandHandlerAsync(SocketCommandBase command)
		{
			var firstOption = ((SocketSlashCommand)command).Data.Options.FirstOrDefault();
			string firstParameter = firstOption?.Value.ToString();
			string eventName;
			switch (command.CommandName)
			{
				case UpdateCommandName:
					await command.RespondAsync("Leaderboard updating!");
					await Leaderboard.UpdateLeaderboardsAsync(_host.Services);
					await command.ModifyOriginalResponseAsync(a => a.Content = "Leaderboard updated!");
					break;
				case CreateEventCommandName:
					eventName = firstParameter;
					await command.RespondAsync($"Creating event \"{eventName}\"");
					await EventHandler.CreateEvent(eventName);
					await command.ModifyOriginalResponseAsync(a => a.Content = $"Created event \"{eventName}\"");
					_config = await Config.GetConfigAsync();
					break;
				case ListLeaderboardCommandName:
					eventName = firstParameter;
					var leaderboard = await LeaderboardParser.LoadLeaderboardAsync(eventName, _host.Services);
					string message = await leaderboard.Leaderboard.ToStringWithUsernames();
					await command.RespondAsync(message);
					break;
				default:
					throw new Exception("Command not handled!");
			}
		}
	}
}