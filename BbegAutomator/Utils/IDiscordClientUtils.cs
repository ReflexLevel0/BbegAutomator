using Discord;
using Discord.WebSocket;

namespace BbegAutomator.Utils;

public interface IDiscordClientUtils
{
    /// <summary>
    /// Returns a channel in which bbeg's and leaderboards are posted
    /// </summary>
    /// <returns></returns>
    Task<ISocketMessageChannel> GetBbegChannel();

    /// <summary>
    /// Returns a channel in which bump commands are located
    /// </summary>
    /// <returns></returns>
    Task<ISocketMessageChannel> GetBumpChannel();

    /// <summary>
    /// Returns messages from the specified channel
    /// </summary>
    /// <param name="channel">Channel from which messages are being returned</param>
    /// <param name="maxMessagesCount">Max number of messages that will be returned</param>
    /// <returns></returns>
    Task<List<IMessage>> GetChannelMessages(IMessageChannel channel, int maxMessagesCount = 100);
}