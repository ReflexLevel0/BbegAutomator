using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class Program
	{
		private DiscordSocketClient _client;
		private const int DeleteMessagesCount = 3;
		private const ulong BumpChannelId = 1023588758068662352;
		private const ulong BbegChannelId = 1024724677328900106;
		private const ulong BumpBotId = 1023365731523498034;
		private const string BumpCommandString = "help";

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
			//Returning if this message isn't an interaction (so for example, a slash command) 
			if (message.Interaction == null) return;

			//Checking if the user has used the help command
			if (message.Channel.Id != BumpChannelId) return;
			if (string.Compare(message.Interaction.Name, BumpCommandString, StringComparison.Ordinal) != 0) return;
			await Console.Out.WriteLineAsync($"{message.Interaction.User.Username} used the help command!!");

			//Going through all of the messages (messages are retrieved in collections)
			var messages = await ChannelUtils.GetMessages(_client, BumpChannelId);

			//If there are less then N messages then messages in the channel wont be deleted
			if (messages.Count < 3) return;

			//Going through each message, updating the leaderboard each time and deleting the message (except the last one)
			foreach (var channelMessage in messages.Skip(1))
			{
				//TODO: updating the leaderboard if this message is the bump command 
				if (channelMessage.Author.Id != BumpBotId) continue;
				if (channelMessage.Interaction != null && string.CompareOrdinal(channelMessage.Interaction.Name, BumpCommandString) == 0)
				{
					ulong userId = channelMessage.Interaction.User.Id;
					//leaderboard.UpdateUser(userId, 1);
					if (channelMessage is not SocketUserMessage userMessage) throw new Exception("");
					await userMessage.ModifyAsync(m => m.Content = "test");
				}

				//Deleting the message
				Console.WriteLine($"Deleting message with id {channelMessage.Id}");
				await channelMessage.DeleteAsync();
				Console.WriteLine($"Deleted message with id {channelMessage.Id}");
				await Task.Delay(1000);
			}
		}
	}
}