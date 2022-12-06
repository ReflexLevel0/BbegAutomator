using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using BbegAutomator.Leaderboard;

namespace BbegAutomator;

public class EventUtils
{
	private readonly IServiceProvider _serviceProvider;
	private readonly Config _config;

	public EventUtils(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_config = (Config)serviceProvider.GetService(typeof(Config));
	}
	
	/// <summary>
	/// Creates a new event with the specified name
	/// </summary>
	/// <param name="eventName">Name of the event to be created</param>
	/// <returns></returns>
	/// <exception cref="EventAlreadyExistsException">Thrown if an event with the same event as <paramref name="eventName"/> already exists</exception>
	/// <exception cref="EventNameNullException">Thrown if <see cref="eventName"/> is null or white space</exception>
	public async Task<Config> CreateEvent(string eventName)
	{
		//Checking the input parameter
		if (EventExists(eventName)) throw new EventAlreadyExistsException(eventName);
		if (string.IsNullOrWhiteSpace(eventName)) throw new EventNameNullException();
		
		//Creating the event file and updating the config
		File.Create(EventNameToFilePath(eventName)).Close();
		_config.CurrentEvent = eventName;
		await _config.UpdateConfigFile();
		return await Config.GetConfig();
	}

	/// <summary>
	/// Renames the specified event to the new name
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="newEventName"></param>
	/// <returns></returns>
	/// <exception cref="EventDoesntExistException">Thrown if event <paramref name="eventName"/> doesn't exist</exception>
	public async Task<Config> RenameEvent(string eventName, string newEventName)
	{
		//Checking if the event exists
		if (EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		
		//Renaming the file
		string oldFileName = EventNameToFilePath(eventName);
		string newFileName = EventNameToFilePath(newEventName);
		if (File.Exists(oldFileName)) File.Move(oldFileName, newFileName);

		//Updating the leaderboard
		await new LeaderboardUtils(_serviceProvider).UpdateLeaderboard(newEventName);
		
		//Changing the config if the name of the current event was changed
		if (string.Compare(eventName, _config.CurrentEvent, StringComparison.Ordinal) != 0) return _config;
		_config.CurrentEvent = newEventName;
		await _config.UpdateConfigFile();
		return _config;
	}

	/// <summary>
	/// Deletes the specified event
	/// </summary>
	/// <param name="eventName"></param>
	/// <exception cref="EventDoesntExistException"></exception>
	public async Task DeleteEvent(string eventName)
	{
		if (EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		
		//Updating the config if current event is being deleted
		if (string.Compare(_config.CurrentEvent, eventName, StringComparison.Ordinal) == 0)
		{
			_config.CurrentEvent = "";
			await _config.UpdateConfigFile();
		} 

		//Deleting the leaderboard message
		var leaderboard = await new LeaderboardUtils(_serviceProvider).LoadLeaderboard(eventName);
		if (leaderboard.MessageId != null)
		{
			var bbegChannel = await new DiscordClientUtils(_serviceProvider).GetBbegChannel();
			await bbegChannel.DeleteMessageAsync((ulong)leaderboard.MessageId);
		}
		
		//Deleting the file
		string fileName = EventNameToFilePath(eventName);
		File.Delete(fileName);
	}
	
	public IEnumerable<string> GetEventNames()
	{
		var files = new DirectoryInfo("data").GetFiles();
		foreach (var file in files)
		{
			string name = file.Name[..^file.Extension.Length];
			yield return name;
		}
	}

	/// <summary>
	/// Converts <paramref name="eventName"/> to 
	/// </summary>
	/// <param name="eventName"></param>
	/// <returns></returns>
	public string EventNameToFilePath(string eventName) => $"data/{eventName}.txt";
	
	public bool EventExists(string eventName) => GetEventNames().Any(name => string.Compare(name, eventName, StringComparison.Ordinal) == 0);
}