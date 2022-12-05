using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;
using Discord.WebSocket;

namespace BbegAutomator;

public static class EventUtils
{
	public static async Task<Config> CreateEventAsync(string eventName, IServiceProvider serviceProvider)
	{
		if (GetEventNames().Any(name => string.Compare(name, eventName, StringComparison.Ordinal) == 0))
		{
			throw new EventAlreadyExistsException(eventName);
		}

		File.Create(FileUtils.EventNameToFilePath(eventName));
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
		if (GetEventNames().Any(e => e.CompareTo(eventName) == 0) == false) 
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

	public static async Task DeleteEventAsync(string eventName, IServiceProvider serviceProvider)
	{
		//Checking if user is trying to delete the current event
		var config = (Config)serviceProvider.GetService(typeof(Config));
		if (config.CurrentEvent.CompareTo(eventName) == 0) throw new ErrorDeletingEvent(); 

		//Deleting the leaderboard message
		var leaderboard = await LeaderboardParser.LoadLeaderboardAsync(eventName, serviceProvider);
		if (leaderboard == null) throw new EventDoesntExistException(eventName);
		if (leaderboard.MessageId != null)
		{
			var client = (IDiscordClient)serviceProvider.GetService(typeof(IDiscordClient));
			var bbegChannel = await client.GetChannelAsync(config.BbegChannelId) as SocketTextChannel;
			await bbegChannel.DeleteMessageAsync((ulong)leaderboard.MessageId);
		}
		
		//Deleting the file
		string fileName = FileUtils.EventNameToFilePath(eventName);
		if (File.Exists(fileName) == false) throw new EventDoesntExistException(eventName);
		File.Delete(fileName);
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