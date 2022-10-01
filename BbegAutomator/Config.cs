using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BbegAutomator
{
	public class Config
	{
		public ulong BumpChannelId = 1023588758068662352;
		public ulong BbegChannelId = 1024724677328900106;
		public ulong BumpBotId = 1023365731523498034;
		public ulong BumpCommandId = 0;

		public static async Task<Config> GetConfig()
		{
			return JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("config.json"));
		}
	}
}