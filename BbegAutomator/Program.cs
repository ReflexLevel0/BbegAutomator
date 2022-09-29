using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
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
			//_client.MessageReceived += OnMessageReceived;
			//_client.SlashCommandExecuted += SlashCommandHandler;


			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			//TODO: remove this temp code
			// var channel = await _client.GetChannelAsync(1024724677328900106); 
			// var textChannel = channel as RestTextChannel;
			// if (textChannel == null) throw new Exception();
			// //var message = await textChannel.SendMessageAsync("ovo je test");
			// var message = await textChannel.GetMessageAsync(1025016469559455774);
			// Console.WriteLine(message.Content);

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
			
			//Parsing the leaderboard
			var leaderboardMessage = await BbegLeaderboard.GetLastLeaderboardMessageAsync(_client, BbegChannelId);
			var leaderboard = BbegLeaderboardParser.ParseLeaderboardMessage(leaderboardMessage);

			//Getting messages in the bump channel (up to 100 messages)
			var channelMessageCollections = message.Channel.GetMessagesAsync();
			
			//Going through all of the messages (messages are retrieved in collections)
			bool firstCollection = true;
			bool firstMessage = true;
			foreach (var messageCollection in await channelMessageCollections.ToListAsync())
			{
				//The first collection should be the biggest, so if it has less then N messages then messages in the channel wont be deleted
				if (firstCollection && messageCollection.Count < DeleteMessagesCount) return;
				firstCollection = false;
				
				//Going through each message, updating the leaderboard each time and deleting the message
				foreach(var channelMessage in messageCollection.ToList())
				{
					//If this is the last message that was sent in the channel,
					//it won't be deleted so other people can see when the last bump was made
					if (firstMessage)
					{
						firstMessage = false;
						continue;
					}

					//TODO: updating the leaderboard if this message is the bump command 
					if(channelMessage.Author.Id != BumpBotId) continue;
					if (channelMessage.Interaction != null && string.CompareOrdinal(channelMessage.Interaction.Name, BumpCommandString) == 0)
					{
						string username = channelMessage.Interaction.User.Username;
						leaderboard.UpdateUser(username, 1);
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
		
		// private async Task SlashCommandHandler(SocketSlashCommand command)
		// {
		// 	await command.RespondAsync($"You executed {command.Data.Name}");
		// }
	}
}