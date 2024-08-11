using System.Text;
using BbegAutomator.Exceptions;
using BbegAutomator.Utils;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace BbegAutomator.Leaderboard;

public class LeaderboardUtils(IServiceProvider serviceProvider) : ILeaderboardUtils
{
	public async Task<List<LeaderboardFileData>> LoadAllLeaderboards()
	{
		var data = new List<LeaderboardFileData>();
		foreach (string eventName in serviceProvider.GetRequiredService<IEventUtils>().GetEventNames())
		{
			data.Add(await LoadLeaderboard(eventName));
		}

		return data;
	}
	
	public async Task<LeaderboardFileData> LoadLeaderboard(string eventName)
	{
		var eventUtils = serviceProvider.GetRequiredService<IEventUtils>();
		if (eventUtils.EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		var leaderboard = serviceProvider.GetRequiredService<ILeaderboard>();
		leaderboard.EventName = eventName;
		var result = new LeaderboardFileData { Leaderboard = leaderboard };
		
		//Not loading any data into the leaderboard if the event file does not exist
		string filePath = eventUtils.EventNameToFilePath(eventName);
		if (File.Exists(filePath) == false)
		{
			return result;
		}
		
		//Reading the file
		string[] lines = await File.ReadAllLinesAsync(filePath);
		if (lines.Length == 0) return result;

		//Parsing file data
		bool firstLine = true;
		foreach (string line in lines)
		{
			if (firstLine)
			{
				result.MessageId = ulong.Parse(line);
				firstLine = false;
			}
			else
			{
				var record = ParseLine(line);
				result.Leaderboard.UpdateUser(record.Id, record.Points);
			}
		}

		return result;
	}
	
	public async Task UpdateLeaderboard(bool skipLastMessage = true)
	{
		var config = serviceProvider.GetRequiredService<IConfig>();
		await UpdateLeaderboard(config.CurrentEvent, skipLastMessage);
	}
	
	public async Task UpdateLeaderboard(string eventName, bool skipLastMessage = true)
	{
		if (serviceProvider.GetRequiredService<IEventUtils>().EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		var client = serviceProvider.GetRequiredService<IDiscordClientUtils>();
		var bbegChannel = await client.GetBbegChannel();
		var bumpChannel = await client.GetBumpChannel();
		
		//Loading leaderboard data
		var leaderboard = await LoadLeaderboard(eventName);
		if (leaderboard == null) throw new EventDoesntExistException(eventName);

		//Going through each message
		var config = serviceProvider.GetRequiredService<IConfig>();
		var messages = await serviceProvider.GetRequiredService<IDiscordClientUtils>().GetChannelMessages(bumpChannel);
		if (skipLastMessage) messages = messages.Skip(1).ToList();
		foreach (var channelMessage in messages)
		{
			//Updating the leaderboard if this message is the bump command 
			if (channelMessage.Author.Id == config.BumpBotId &&
			    channelMessage.Interaction != null &&
			    string.CompareOrdinal(channelMessage.Interaction.Name, config.BumpCommandString) == 0)
			{
				ulong userId = channelMessage.Interaction.User.Id;
				leaderboard.Leaderboard.UpdateUser(userId, 1);
			}

			//Deleting the bump reply message
			await channelMessage.DeleteAsync();
		}
		
		//Not posting any messages or writing anything to a file if the leaderboard is empty
		if (leaderboard.Leaderboard.IsEmpty()) return;
		
		//Creating a new leaderboard message if a message doesn't exist
		ulong messageId = 0;
		if (leaderboard.MessageId is null or 0)
		{
			string leaderboardMessage = await leaderboard.Leaderboard.ToStringWithUsernames();
			if (string.IsNullOrWhiteSpace(leaderboardMessage) == false)
			{
				var message = await bbegChannel.SendMessageAsync(leaderboardMessage);
				messageId = message.Id;
			}
		}

		//Updating the leaderboard message if the message exists
		else
		{
			if (await bbegChannel.GetMessageAsync((ulong)leaderboard.MessageId) is not RestUserMessage message)
			{
				throw new Exception("Error converting discord message to SocketUserMessage type");
			}
			string newContent = await leaderboard.Leaderboard.ToStringWithUsernames();
			await message.ModifyAsync(m => m.Content = newContent);
			messageId = (ulong)leaderboard.MessageId;
		}

		//Writing changes to the file
		await UpdateLeaderboardFile(eventName, leaderboard.Leaderboard, messageId);
	}

	/// <summary>
	/// Updates the leaderboard file with data from <paramref name="leaderboard"/>
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="leaderboard"></param>
	/// <param name="messageId"></param>
	private async Task UpdateLeaderboardFile(string eventName, ILeaderboard leaderboard, ulong messageId)
	{
		var eventUtils = serviceProvider.GetRequiredService<IEventUtils>();
		if (eventUtils.EventExists(eventName) == false) throw new EventDoesntExistException(eventName);
		string filePath = eventUtils.EventNameToFilePath(eventName);
		
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