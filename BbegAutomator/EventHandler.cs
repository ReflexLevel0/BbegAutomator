using System.Threading.Tasks;

namespace BbegAutomator;

public static class EventHandler
{
	public static async Task CreateEvent(string eventName)
	{
		var config = await Config.GetConfigAsync();
		config.CurrentEvent = eventName;
		await config.WriteConfigAsync();
	}
}