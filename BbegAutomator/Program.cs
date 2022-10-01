using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class Program
	{
		//TODO: store this data in the config file
		private DiscordSocketClient _client;

		private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

		private async Task MainAsync()
		{
			_client = new DiscordSocketClient();
			_client.Log += Log;

			string token = await File.ReadAllTextAsync("token.txt");

			//TODO: its unoptimized to do it this way, there isnt a reason to listen for every message, another event like SlashCommandExecuted should be used
			_client.MessageReceived += OnMessageReceived;
			//_client.SlashCommandExecuted += SlashCommandHandler;


			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		private static Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			var config = await Config.GetConfig();
			
			//Checking if the user has used the bump command
			if (message.Interaction == null) return;
			if (message.Channel.Id != config.BumpChannelId) return;
			if (message.Interaction.Id != config.BumpCommandId) return;
			await Console.Out.WriteLineAsync($"{message.Interaction.User.Username} used the bump command!!");
			
			await BbegLeaderboard.UpdateLeaderboardsAsync(_client);
		}
	}
}