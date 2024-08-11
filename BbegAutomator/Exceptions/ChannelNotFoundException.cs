namespace BbegAutomator.Exceptions;

public class ChannelNotFoundException(ulong channelId) : Exception($"Channel ID {channelId} not found!")
{
    
}