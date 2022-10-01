using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class BbegLeaderboard
	{
		private readonly List<BbegLeaderboardRecord> _leaderboard = new List<BbegLeaderboardRecord>();
		public IReadOnlyList<BbegLeaderboardRecord> Leaderboard => _leaderboard;

		/// <summary>
		/// Adds <exception cref="pointsToAdd"> number of points to the user with the specified id</exception>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="pointsToAdd"></param>
		public void UpdateUser(ulong id, int pointsToAdd)
		{
			var user = Leaderboard.FirstOrDefault(r => r.Id == id);
			if (user == null)
			{
				_leaderboard.Add(new BbegLeaderboardRecord(id, pointsToAdd));
			}
			else
			{
				user.Points += pointsToAdd;
			}
		}

		/// <summary>
		/// Reads all messages in the bump channel and updates the leaderboards 
		/// </summary>
		/// <param name="client"></param>
		/// <param name="skipLastMessage">Skips the last bump command in the channel if true</param>
		/// <exception cref="Exception"></exception>
		public static async Task UpdateLeaderboardsAsync(DiscordSocketClient client, bool skipLastMessage = true)
		{
			var config = await Config.GetConfig();
			
			//Going through each message, updating the leaderboard each time and deleting the message (except the last one)
			var messages = await ChannelUtils.GetMessages(client, config.BumpChannelId);
			if (skipLastMessage) messages = messages.Skip(1).ToList();
			foreach (var channelMessage in messages)
			{
				//TODO: updating the leaderboard if this message is the bump command 
				if (channelMessage.Author.Id == config.BumpBotId && channelMessage.Interaction != null && channelMessage.Interaction.Id == config.BumpCommandId)
				{
					ulong userId = channelMessage.Interaction.User.Id;
					var messageCreationDate = channelMessage.CreatedAt.UtcDateTime;
					var leaderboard = BbegLeaderboardParser.LoadLeaderboard(messageCreationDate.Year, messageCreationDate.Month);
					leaderboard.UpdateUser(userId, 1);
					//TODO: what if leaderboard is null
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