namespace BbegAutomator;

public interface IConfig
{
    ulong BumpChannelId { get; }
    ulong BbegChannelId { get; }
    ulong BumpBotId { get; }
    string BumpCommandString { get; }
    string BotToken { get; }
    List<ulong> LoggingIds { get; } 
    string CurrentEvent { get; set; }
    ulong GuildId { get; }
    
    /// <summary>
    /// Updates the configuration file
    /// </summary>
    Task UpdateConfigFile();
}
