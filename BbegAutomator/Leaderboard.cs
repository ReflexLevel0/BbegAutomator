using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class Leaderboard
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly List<LeaderboardRecord> _records = new List<LeaderboardRecord>();
		public IReadOnlyList<LeaderboardRecord> Records => _records;

		public Leaderboard(IServiceProvider servicesProvider)
		{
			_serviceProvider = servicesProvider;
		}
		
		/// <summary>
		/// Adds <exception cref="pointsToAdd"> number of points to the user with the specified id</exception>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="pointsToAdd"></param>
		public void UpdateUser(ulong id, int pointsToAdd)
		{
			var user = Records.FirstOrDefault(r => r.Id == id);
			if (user == null)
			{
				_records.Add(new LeaderboardRecord(id, pointsToAdd));
			}
			else
			{
				user.Points += pointsToAdd;
			}
		}

		public override string ToString()
		{
			var builder = new StringBuilder(1024);
			foreach (var r in _records)
			{
				builder.AppendLine($"{r.Id} {r.Points}");
			}
			return builder.ToString();
		}

		public async Task<string> ToStringWithUsernames()
		{
			var client = (IDiscordClient)_serviceProvider.GetService(typeof(IDiscordClient));
			if (client == null) throw new DependencyInjectionNullException();
			
			var builder = new StringBuilder(1024);
			foreach (var r in _records)
			{
				var user = await client.GetUserAsync(r.Id);
				builder.AppendLine($"{user.Mention} {r.Points}");
			}

			return builder.ToString();
		}

		/// <summary>
		/// Reads all messages in the bump channel and updates the leaderboards 
		/// </summary>
		/// <param name="serviceProvider"></param>
		/// <param name="skipLastMessage">Skips the last bump command in the channel if true</param>
		/// <exception cref="Exception"></exception>
		public static async Task UpdateLeaderboardsAsync(IServiceProvider serviceProvider, bool skipLastMessage = true)
		{
			await Program.Log(new LogMessage(LogSeverity.Info, null, "Updating leaderboard"));

			var config = (Config) serviceProvider.GetService(typeof(Config));
			var client = (IDiscordClient) serviceProvider.GetService(typeof(IDiscordClient));
			if (client == null || config == null) throw new DependencyInjectionNullException();

			var channel = await client.GetChannelAsync(config.BbegChannelId);
			if (channel is not SocketTextChannel bbegChannel) 
				throw new Exception("Bbeg channel is null!");
			
			//Going through each message, updating the leaderboard each time and deleting the message (except the last one)
			var messages = await ChannelUtils.GetMessages(serviceProvider, config.BumpChannelId);
			if (skipLastMessage) messages = messages.Skip(1).ToList();
			foreach (var channelMessage in messages)
			{
				//Updating the leaderboard if this message is the bump command 
				if (channelMessage.Author.Id == config.BumpBotId && 
				    channelMessage.Interaction != null && 
				    string.CompareOrdinal(channelMessage.Interaction.Name, config.BumpCommandString) == 0)
				{
					//Loading and updating the leaderboard data
					ulong userId = channelMessage.Interaction.User.Id;
					var messageCreationDate = channelMessage.CreatedAt.UtcDateTime;
					var leaderboardFile = await LeaderboardParser.LoadLeaderboardAsync(messageCreationDate.Year, messageCreationDate.Month, serviceProvider) ?? 
					                      new LeaderboardFileData { Leaderboard = new Leaderboard(serviceProvider)};
					leaderboardFile.Leaderboard.UpdateUser(userId, 1);

					//Creating a new leaderboard message if a message doesn't exist
					ulong messageId;
					if (leaderboardFile.MessageId == null)
					{
						var message = await bbegChannel.SendMessageAsync(leaderboardFile.Leaderboard.ToString());
						messageId = message.Id;
					}
					
					//Updating the leaderboard message if the message exists
					else
					{
						var message = await bbegChannel.GetMessageAsync((ulong) leaderboardFile.MessageId) as RestUserMessage;
						if (message == null) throw new Exception("Error converting discord message to SocketUserMessage type");
						string newContent = await leaderboardFile.Leaderboard.ToStringWithUsernames();
						await message.ModifyAsync(m => m.Content = newContent);
						messageId = (ulong) leaderboardFile.MessageId;
					}

					//Writing changes to the file
					await LeaderboardParser.WriteLeaderboardAsync(messageCreationDate.Year, messageCreationDate.Month, leaderboardFile.Leaderboard, messageId);
				}

				//Deleting the message
				await Program.Log(new LogMessage(LogSeverity.Verbose, null, $"Deleting message with id {channelMessage.Id}"));
				await channelMessage.DeleteAsync();
			}
			
			await Program.Log(new LogMessage(LogSeverity.Info, null, "Leaderboard updated"));
		}
	}
}