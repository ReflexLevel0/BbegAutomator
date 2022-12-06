using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator;

public class DiscordClientUtils
{
	private readonly Config _config;
	private readonly IDiscordClient _client;

	public DiscordClientUtils(IServiceProvider serviceProvider)
	{
		_config = (Config)serviceProvider.GetService(typeof(Config));
		_client = (IDiscordClient)serviceProvider.GetService(typeof(IDiscordClient));
	}

	/// <summary>
	/// Returns a channel in which bbeg's and leaderboards are posted
	/// </summary>
	/// <returns></returns>
	public async Task<ISocketMessageChannel> GetBbegChannel() => await GetChannel(_config.BbegChannelId);
	
	/// <summary>
	/// Returns a channel in which bump commands are located
	/// </summary>
	/// <returns></returns>
	public async Task<ISocketMessageChannel> GetBumpChannel() => await GetChannel(_config.BumpChannelId);

	/// <summary>
	/// Returns messages from the specified channel
	/// </summary>
	/// <param name="channel">Channel from which messages are being returned</param>
	/// <param name="maxMessagesCount">Max number of messages that will be returned</param>
	/// <returns></returns>
	public static async Task<List<IMessage>> GetChannelMessages(IMessageChannel channel, int maxMessagesCount = 100)
	{
		var messages = new List<IMessage>();
		var channelMessageCollections = channel.GetMessagesAsync(maxMessagesCount);
		foreach (var messageCollection in await channelMessageCollections.ToListAsync())
		{
			messages.AddRange(messageCollection);
		}
		return messages;
	}
	
	private async Task<SocketTextChannel> GetChannel(ulong channelId) => await _client.GetChannelAsync(channelId) as SocketTextChannel;
}