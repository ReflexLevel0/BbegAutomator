using BbegAutomator.Exceptions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace BbegAutomator.Utils;

public class DiscordClientUtils(IServiceProvider serviceProvider) : IDiscordClientUtils
{
	public async Task<ISocketMessageChannel> GetBbegChannel()
	{
		var config = serviceProvider.GetRequiredService<IConfig>();
		return await GetChannel(config.BbegChannelId);
	}
	
	public async Task<ISocketMessageChannel> GetBumpChannel()
	{
		var config = serviceProvider.GetRequiredService<IConfig>();
		return await GetChannel(config.BumpChannelId);
	}
	
	public async Task<List<IMessage>> GetChannelMessages(IMessageChannel channel, int maxMessagesCount = 100)
	{
		var messages = new List<IMessage>();
		var channelMessageCollections = channel.GetMessagesAsync(maxMessagesCount);
		foreach (var messageCollection in await channelMessageCollections.ToListAsync())
		{
			messages.AddRange(messageCollection);
		}
		return messages;
	}

	private async Task<SocketTextChannel> GetChannel(ulong channelId)
	{
		var client = serviceProvider.GetRequiredService<IDiscordClient>();
		var channel = await client.GetChannelAsync(channelId);
		if (channel is not SocketTextChannel textChannel)
		{
			throw new ChannelNotFoundException(channelId);
		}
		return textChannel;
	}
}