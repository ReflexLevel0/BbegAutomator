using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class Program
	{
		//TODO: store this data in the config file
		private static DiscordSocketClient _client;

		//TODO: crash the program if an exception occurs 
		private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

		private async Task MainAsync()
		{
			_client = new DiscordSocketClient();
			_client.Log += Log;

			//TODO: its unoptimized to do it this way, there isn't a reason to listen for every message, another event like SlashCommandExecuted should be used
			_client.MessageReceived += OnMessageReceived;
			//_client.SlashCommandExecuted += SlashCommandHandler;


			await _client.LoginAsync(TokenType.Bot, (await Config.GetConfigAsync()).BotToken);
			await _client.StartAsync();

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		public static async Task Log(LogMessage msg)
		{
			var config = await Config.GetConfigAsync();
			foreach (ulong id in config.LoggingIds)
			{
				var user = await _client.GetUserAsync(id);
				await user.SendMessageAsync(msg.Message);
			}
			Console.WriteLine(msg.ToString());
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			var config = await Config.GetConfigAsync();
			
			//Checking if the user has used the bump command
			if (message.Interaction == null) return;
			if (message.Channel.Id != config.BumpChannelId) return;
			await Log(new LogMessage(LogSeverity.Info, null, $"{message.Interaction.User.Username} used a command in the bump channel!!"));
			
			await Leaderboard.UpdateLeaderboardsAsync(_client);
		}
	}
}