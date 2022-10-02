using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BbegAutomator
{
	public class Program
	{
		//TODO: crash the program if an exception occurs 
		private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

		private IHost _host;
		private static readonly DiscordSocketClient Client = new DiscordSocketClient();
		private static Config _config;
		
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

			//TODO: its unoptimized to do it this way, there isn't a reason to listen for every message, another event like SlashCommandExecuted should be used
			Client.MessageReceived += OnMessageReceived;
			//_client.SlashCommandExecuted += SlashCommandHandler;
			
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
	}
}