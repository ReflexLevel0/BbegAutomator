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
		/// <param name="bumpChannelId"></param>
		/// <param name="bumpBotId"></param>
		/// <param name="bumpCommandId"></param>
		/// <param name="skipLastMessage">Skips the last bump command in the channel if true</param>
		/// <exception cref="Exception"></exception>
		public static async Task UpdateLeaderboardsAsync(DiscordSocketClient client, ulong bumpChannelId, ulong bumpBotId, ulong bumpCommandId, bool skipLastMessage = true)
		{
			//Going through all of the messages (messages are retrieved in collections)
			var messages = await ChannelUtils.GetMessages(client, bumpChannelId);
			if (messages.Count < 3) return;

			//Going through each message, updating the leaderboard each time and deleting the message (except the last one)
			if (skipLastMessage) messages = messages.Skip(1).ToList();
			foreach (var channelMessage in messages)
			{
				//TODO: updating the leaderboard if this message is the bump command 
				if (channelMessage.Author.Id == bumpBotId && channelMessage.Interaction != null && channelMessage.Interaction.Id == bumpCommandId)
				{
					ulong userId = channelMessage.Interaction.User.Id;
					var messageCreationDate = channelMessage.CreatedAt.UtcDateTime;
					string filePath = $"data/{messageCreationDate.Year}-{messageCreationDate.Month, 2}";
					if (File.Exists(filePath))
					{
						string[] lines = await File.ReadAllLinesAsync(filePath);
						ulong messageId = Convert.ToUInt64(lines[0]);
						var leaderboard = new BbegLeaderboard();
						foreach (string line in lines.Skip(1))
						{
							string[] lineParts = line.Split(" ");
							var record = new BbegLeaderboardRecord(Convert.ToUInt64(lineParts[0]), int.Parse(lineParts[1]));
							leaderboard.UpdateUser(record.Id, record.Points);
						}
					}

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