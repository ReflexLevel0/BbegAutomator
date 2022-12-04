using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;

namespace BbegAutomator;

public static class EventHandler
{
	public static async Task CreateEvent(string eventName, IServiceProvider serviceProvider)
	{
		if (GetEventNames().Any(name => string.Compare(name, eventName, StringComparison.Ordinal) == 0))
		{
			throw new EventAlreadyExistsException(eventName);
		}
		var config = (Config)serviceProvider.GetService(typeof(Config));
		if (config is null) throw new Exception("DI exception");
		config.CurrentEvent = eventName;
		await config.WriteConfigAsync();
	}

	public static IEnumerable<string> GetEventNames()
	{
		var files = new DirectoryInfo("data").GetFiles();
		foreach(var file in files)
		{
			string name = file.Name[..^file.Extension.Length];
			yield return name;
		}
	}
}