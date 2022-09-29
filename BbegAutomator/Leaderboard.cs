using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class BbegLeaderboard
	{
		public List<BbegLeaderboardRecord> Leaderboard { get; } = new List<BbegLeaderboardRecord>();

		public void UpdateUser(ulong id, int pointsToAdd)
		{
			var user = Leaderboard.FirstOrDefault(r => r.Id == id);
			if (user == null)
			{
				Leaderboard.Add(new BbegLeaderboardRecord(id, pointsToAdd));
			}
			else
			{
				user.Points += pointsToAdd;
			}
		}

		public static async Task<RestMessage> GetLastLeaderboardMessageAsync(DiscordSocketClient client, ulong bbegChannelId)
		{
			var bbegChannel = (ISocketMessageChannel) client.GetChannel(bbegChannelId);
			var pinnedMessages = bbegChannel.GetPinnedMessagesAsync();
			var leaderboardMessage = (await pinnedMessages).FirstOrDefault();
			if (leaderboardMessage == null) throw new Exception("There are no pinned messages!");
			Console.WriteLine($"Leaderboard message: {leaderboardMessage.Content}");
			return leaderboardMessage;
		}
	}
}