using Newtonsoft.Json;

namespace BbegAutomator;

public class Config : IConfig
{
	public ulong BumpChannelId { get; set; }
	public ulong BbegChannelId { get; set; }
	public ulong BumpBotId { get; set; }
	public string BumpCommandString { get; set; }
	public string BotToken { get; set; }
	public List<ulong> LoggingIds { get; set; } = new List<ulong>();
	public string CurrentEvent { get; set; }
	public ulong GuildId { get; set; }
	private const string AppSettingsPath = "appsettings.json"; 

	public Config()
	{
		
	}
	
	public Config(ulong bumpChannelId, ulong bbegChannelId, ulong bumpBotId, string bumpCommandString, string botToken, List<ulong> loggingIds, string currentEvent, ulong guildId)
	{
		BumpChannelId = bumpChannelId;
		BbegChannelId = bbegChannelId;
		BumpBotId = bumpBotId;
		BumpCommandString = bumpCommandString;
		BotToken = botToken;
		LoggingIds = loggingIds;
		CurrentEvent = currentEvent;
		GuildId = guildId;
	}

  /// <summary>
  /// Loads settings from configuration file
  /// </summary>
  /// <returns></returns>
	public static async Task<IConfig> LoadFromFile()
	{
		string data = await File.ReadAllTextAsync(AppSettingsPath);
		var config = JsonConvert.DeserializeObject<Config>(data);
		if (config == null) throw new FileNotFoundException(AppSettingsPath);
		return config;
	}
	
	public async Task UpdateConfigFile()
	{
		string serializedConfig = JsonConvert.SerializeObject(this);
		await File.WriteAllTextAsync(AppSettingsPath, serializedConfig);
	} 
}
