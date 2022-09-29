using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator
{
	public static class ChannelUtils
	{
		/// <summary>
		/// Returns messages from the specified channel
		/// </summary>
		/// <param name="client">Discord client</param>
		/// <param name="channelId">Id of the channel which messages are being returned</param>
		/// <param name="maxMessagesCount">Max number of messages that will be returned</param>
		/// <returns></returns>
		public static async Task<List<IMessage>> GetMessages(DiscordSocketClient client, ulong channelId, int maxMessagesCount = 100)
		{
			var messages = new List<IMessage>();
			var channel = await client.GetChannelAsync(channelId) as ISocketMessageChannel;
			if (channel == null) throw new Exception($"Channel with id {channelId} not found!");
			var channelMessageCollections = channel.GetMessagesAsync();
			foreach (var messageCollection in await channelMessageCollections.ToListAsync())
			{
				messages.AddRange(messageCollection);
			}
			return messages;
		}
	}
}