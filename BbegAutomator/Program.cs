using System;
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

		private async Task OnMessageReceived(SocketMessage message)
		{
			//Checking if the user has used the bump command
			if (message.Interaction == null) return;
			if (message.Channel.Id != _config.BumpChannelId) return;
			await Log(new LogMessage(LogSeverity.Info, null, $"{message.Interaction.User.Username} used a command in the bump channel!!"));

			await Leaderboard.UpdateLeaderboardsAsync(_host.Services);
		}

		private async Task InitCommandsAsync()
		{
			{
				//Setting up app commands
				var guild = Client.GetGuild(_config.GuildId);
				var updateCommand = new SlashCommandBuilder();
				updateCommand.WithName(UpdateCommandName);
				updateCommand.WithDescription("Updates the bbeg leaderboard for the current event");
				try
				{
					await guild.CreateApplicationCommandAsync(updateCommand.Build());
				}
				catch (ApplicationCommandException exception)
				{
					Console.WriteLine(exception.Message);
				}
			}
		}

		private async Task SlashCommandHandlerAsync(SocketCommandBase command)
		{
			if (string.CompareOrdinal(command.CommandName, UpdateCommandName) == 0)
			{
				await command.RespondAsync("Leaderboard updating!");
				await Leaderboard.UpdateLeaderboardsAsync(_host.Services);
			}
			else
			{
				throw new Exception("Command not handled!");
			}
		}
	}
}