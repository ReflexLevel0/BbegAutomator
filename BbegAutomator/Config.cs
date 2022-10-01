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

		public Config(ulong bumpChannelId, ulong bbegChannelId, ulong bumpBotId, string bumpCommandString, string botToken)
		{
			BumpChannelId = bumpChannelId;
			BbegChannelId = bbegChannelId;
			BumpBotId = bumpBotId;
			BumpCommandString = bumpCommandString;
			BotToken = botToken;
		}

		public static async Task<Config> GetConfig()
		{
			return JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("appsettings.json"));
		}
	}
}