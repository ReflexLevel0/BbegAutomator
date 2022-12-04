using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BbegAutomator
{
	public class Config
	{
		public readonly ulong BumpChannelId;
		public readonly ulong BbegChannelId;
		public readonly ulong BumpBotId;
		public readonly string BumpCommandString;
		public readonly string BotToken;
		public readonly List<ulong> LoggingIds;
		public readonly string CurrentEvent;
		public readonly ulong GuildId;

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

		public static async Task<Config> GetConfigAsync()
		{
			return JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("appsettings.json"));
		}
	}
}