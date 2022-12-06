using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord.Rest;

namespace BbegAutomator.Leaderboard;

public class LeaderboardUtils
{
	private readonly IServiceProvider _serviceProvider;
	private readonly Config _config;

	public LeaderboardUtils(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_config = (Config)serviceProvider.GetService(typeof(Config));
	}
	
	/// <summary>
	/// Returns all leaderboards
	/// </summary>
	/// <returns></returns>
	public async Task<List<LeaderboardFileData>> LoadAllLeaderboards()
	{
		var data = new List<LeaderboardFileData>();
		foreach (string eventName in new EventUtils(_serviceProvider).GetEventNames())
		{
			data.Add(await LoadLeaderboard(eventName));
		}

		return data;
	}

	/// <summary>
	/// Parses the leaderboard file for the specified event 
	/// </summary>
	/// <param name="eventName"></param>
	/// <returns>Leaderboard with data from the specified year and month (or null if file doesn't exist)</returns>
	/// <exception cref="EventDoesntExistException"></exception>
	public async Task<LeaderboardFileData> LoadLeaderboard(string eventName)
	{
		if (new EventUtils(_serviceProvider).EventExists(eventName) == false) throw new EventDoesntExistException(eventName);

		//Reading the file
		string filePath = new EventUtils(_serviceProvider).EventNameToFilePath(eventName);
		var fileData = new LeaderboardFileData { Leaderboard = new Leaderboard(eventName, _serviceProvider) };
		string[] lines = await File.ReadAllLinesAsync(filePath);
		if (lines.Length == 0) return fileData;

		//Parsing file data
		bool firstLine = true;
		foreach (string line in lines)
		{
			if (firstLine)
			{
				fileData.MessageId = ulong.Parse(line);
				firstLine = false;
			}
			else
			{
				var record = ParseLine(line);
				fileData.Leaderboard.UpdateUser(record.Id, record.Points);
			}
		}

		return fileData;
	}

	/// <summary>
	/// Reads all messages in the bump channel and updates the leaderboard for the current event
	/// </summary>
	/// <param name="skipLastMessage">Skips the last bump command in the channel if true</param>
	/// <exception cref="EventDoesntExistException"></exception>
	public async Task UpdateLeaderboard(bool skipLastMessage = true)
	{
		await UpdateLeaderboard(_config.CurrentEvent, skipLastMessage);
	}

	/// <summary>
	/// Reads all messages in the bump channel and updates the leaderboard for the specified event
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="skipLastMessage"></param>
	/// <exception cref="EventDoesntExistException"></exception>
	public async Task UpdateLeaderboard(string eventName, bool skipLastMessage = true)
	{
		if (new EventUtils(_serviceProvider).EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		var client = new DiscordClientUtils(_serviceProvider);
		var bbegChannel = await client.GetBbegChannel();
		var bumpChannel = await client.GetBumpChannel();
		
		//Loading leaderboard data
		var leaderboardFile = await LoadLeaderboard(eventName);
		if (leaderboardFile == null) throw new EventDoesntExistException(eventName);

		//Going through each message
		var messages = await DiscordClientUtils.GetChannelMessages(bumpChannel);
		if (skipLastMessage) messages = messages.Skip(1).ToList();
		foreach (var channelMessage in messages)
		{
			//Updating the leaderboard if this message is the bump command 
			if (channelMessage.Author.Id == _config.BumpBotId &&
			    channelMessage.Interaction != null &&
			    string.CompareOrdinal(channelMessage.Interaction.Name, _config.BumpCommandString) == 0)
			{
				ulong userId = channelMessage.Interaction.User.Id;
				leaderboardFile.Leaderboard.UpdateUser(userId, 1);
			}

			//Deleting the message
			await channelMessage.DeleteAsync();
		}

		//Creating a new leaderboard message if a message doesn't exist
		ulong messageId = 0;
		if (leaderboardFile.MessageId is null or 0)
		{
			string leaderboardMessage = await leaderboardFile.Leaderboard.ToStringWithUsernames();
			if (string.IsNullOrWhiteSpace(leaderboardMessage) == false)
			{
				var message = await bbegChannel.SendMessageAsync(leaderboardMessage);
				messageId = message.Id;
			}
		}

		//Updating the leaderboard message if the message exists
		else
		{
			if (await bbegChannel.GetMessageAsync((ulong)leaderboardFile.MessageId) is not RestUserMessage message)
			{
				throw new Exception("Error converting discord message to SocketUserMessage type");
			}
			string newContent = await leaderboardFile.Leaderboard.ToStringWithUsernames();
			await message.ModifyAsync(m => m.Content = newContent);
			messageId = (ulong)leaderboardFile.MessageId;
		}

		//Writing changes to the file
		await UpdateLeaderboardFile(eventName, leaderboardFile.Leaderboard, messageId);
	}

	/// <summary>
	/// Updates the leaderboard file with data from <paramref name="leaderboard"/>
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="leaderboard"></param>
	/// <param name="messageId"></param>
	private async Task UpdateLeaderboardFile(string eventName, Leaderboard leaderboard, ulong messageId)
	{
		if (new EventUtils(_serviceProvider).EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		string filePath = new EventUtils(_serviceProvider).EventNameToFilePath(eventName);
		
		//Making the updated leaderboard message
		var builder = new StringBuilder(1024);
		builder.AppendLine(messageId.ToString());
		builder.Append(leaderboard);

		//Updating the file
		Directory.CreateDirectory("data");
		await File.WriteAllTextAsync(filePath, builder.ToString());
	}
	
	private LeaderboardRecord ParseLine(string line)
	{
		string[] parts = line.Split(" ");
		return new LeaderboardRecord(Convert.ToUInt64(parts[0]), Convert.ToInt32(parts[1]));
	}
}