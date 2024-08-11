using BbegAutomator.Exceptions;
using BbegAutomator.Leaderboard;
using Microsoft.Extensions.DependencyInjection;

namespace BbegAutomator.Utils;

public class EventUtils(IServiceProvider serviceProvider) : IEventUtils
{
	private const string DataDirectoryName = "data";
	
	public async Task<IConfig> CreateEvent(string eventName)
	{
		//Checking the input parameter
		if (EventExists(eventName)) throw new EventAlreadyExistsException(eventName);
		if (string.IsNullOrWhiteSpace(eventName)) throw new EventNameNullException();
		
		//Creating the event file and updating the config
		File.Create(EventNameToFilePath(eventName)).Close();
		var config = serviceProvider.GetRequiredService<IConfig>();
		config.CurrentEvent = eventName;
		await config.UpdateConfigFile();
		return await config.LoadFromFile();
	}
	
	public async Task<IConfig> RenameEvent(string eventName, string newEventName)
	{
		//Checking if the event exists
		if (EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		
		//Renaming the file
		string oldFileName = EventNameToFilePath(eventName);
		string newFileName = EventNameToFilePath(newEventName);
		if (File.Exists(oldFileName)) File.Move(oldFileName, newFileName);

		//Updating the leaderboard
		await new LeaderboardUtils(serviceProvider).UpdateLeaderboard(newEventName);
		
		//Changing the config if the name of the current event was changed
		var config = serviceProvider.GetRequiredService<IConfig>();
		if (string.Compare(eventName, config.CurrentEvent, StringComparison.Ordinal) != 0) return config;
		config.CurrentEvent = newEventName;
		await config.UpdateConfigFile();
		return config;
	}
	
	public async Task DeleteEvent(string eventName)
	{
		if (EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		
		//Updating the config if current event is being deleted
		var config = serviceProvider.GetRequiredService<IConfig>();
		if (string.Compare(config.CurrentEvent, eventName, StringComparison.Ordinal) == 0)
		{
			config.CurrentEvent = "";
			await config.UpdateConfigFile();
		} 

		//Deleting the leaderboard message
		var leaderboard = await serviceProvider.GetRequiredService<ILeaderboardUtils>().LoadLeaderboard(eventName);
		if (leaderboard.MessageId != null)
		{
			var bbegChannel = await serviceProvider.GetRequiredService<IDiscordClientUtils>().GetBbegChannel();
			await bbegChannel.DeleteMessageAsync((ulong)leaderboard.MessageId);
		}
		
		//Deleting the file
		string fileName = EventNameToFilePath(eventName);
		File.Delete(fileName);
	}
	
	public IEnumerable<string> GetEventNames()
	{
		if (Directory.Exists(DataDirectoryName) == false)
		{
			Directory.CreateDirectory(DataDirectoryName);
		}

		var config = serviceProvider.GetRequiredService<IConfig>();
		bool currentEventReturned = false;
		var files = new DirectoryInfo(DataDirectoryName).GetFiles();
		foreach (var file in files)
		{
			string name = file.Name[..^file.Extension.Length];
			if (currentEventReturned == false && string.CompareOrdinal(config.CurrentEvent, name) == 0)
			{
				currentEventReturned = true;
			}
			yield return name;
		}

		if (currentEventReturned == false && config.CurrentEvent.Length != 0)
		{
			yield return config.CurrentEvent;
		}
	}
	
	public string EventNameToFilePath(string eventName) => $"{DataDirectoryName}/{eventName}.txt";
	
	public bool EventExists(string eventName) => GetEventNames().Any(name => string.Compare(name, eventName, StringComparison.Ordinal) == 0);
}