using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;

namespace BbegAutomator;

public static class EventHandler
{
	public static async Task<Config> CreateEventAsync(string eventName, IServiceProvider serviceProvider)
	{
		if (GetEventNames().Any(name => string.Compare(name, eventName, StringComparison.Ordinal) == 0))
		{
			throw new EventAlreadyExistsException(eventName);
		}

		var config = (Config)serviceProvider.GetService(typeof(Config));
		if (config is null) throw new Exception("DI exception");
		config.CurrentEvent = eventName;
		await config.WriteConfigAsync();
		return await Config.GetConfigAsync();
	}

	public static async Task<Config> RenameEventAsync(string eventName, string newEventName, IServiceProvider serviceProvider)
	{
		var config = (Config)serviceProvider.GetService(typeof(Config));
		string oldFileName = FileUtils.EventNameToFilePath(eventName);
		string newFileName = FileUtils.EventNameToFilePath(newEventName);

		//Checking if the event exists
		if (GetEventNames().Any(e => e.CompareTo(eventName) == 0) == false && config.CurrentEvent.CompareTo(eventName) != 0) 
			throw new EventDoesntExistException(eventName);
		
		//Renaming the file
		if (File.Exists(oldFileName)) File.Move(oldFileName, newFileName);

		//Updating the leaderboard
		await Leaderboard.UpdateLeaderboardAsync(newEventName, serviceProvider);
		
		//Changing the config if the name of the current event was changed
		if (string.Compare(eventName, config.CurrentEvent, StringComparison.Ordinal) != 0) return config;
		config.CurrentEvent = newEventName;
		await config.WriteConfigAsync();

		return config;
	}

	public static IEnumerable<string> GetEventNames()
	{
		var files = new DirectoryInfo("data").GetFiles();
		foreach (var file in files)
		{
			string name = file.Name[..^file.Extension.Length];
			yield return name;
		}
	}
}