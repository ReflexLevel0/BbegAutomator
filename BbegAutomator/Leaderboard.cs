﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace BbegAutomator
{
	public class Leaderboard
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly List<LeaderboardRecord> _records = new List<LeaderboardRecord>();
		private IEnumerable<LeaderboardRecord> Records => _records;
		private readonly string _name;

		public Leaderboard(string name, IServiceProvider servicesProvider)
		{
			_serviceProvider = servicesProvider;
			_name = name;
		}
		
		/// <summary>
		/// Adds <exception cref="pointsToAdd"> number of points to the user with the specified id</exception>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="pointsToAdd"></param>
		public void UpdateUser(ulong id, int pointsToAdd)
		{
			var user = Records.FirstOrDefault(r => r.Id == id);
			if (user == null)
			{
				_records.Add(new LeaderboardRecord(id, pointsToAdd));
			}
			else
			{
				user.Points += pointsToAdd;
			}
		}

		public override string ToString()
		{
			var builder = new StringBuilder(1024);
			foreach (var r in _records)
			{
				builder.AppendLine($"{r.Id} {r.Points}");
			}
			return builder.ToString();
		}

		public async Task<string> ToStringWithUsernamesAsync()
		{
			var client = (IDiscordClient)_serviceProvider.GetService(typeof(IDiscordClient));
			if (client == null) throw new DependencyInjectionNullException();
			
			var builder = new StringBuilder(1024);
			builder.AppendLine($"Leaderboard for event \"{_name}\":");
			foreach (var r in _records)
			{
				var user = await client.GetUserAsync(r.Id);
				builder.AppendLine($"{user.Mention} {r.Points}");
			}

			return builder.ToString();
		}

		/// <summary>
		/// Reads all messages in the bump channel and updates the leaderboard for the current event
		/// </summary>
		/// <param name="serviceProvider"></param>
		/// <param name="skipLastMessage">Skips the last bump command in the channel if true</param>
		/// <exception cref="Exception"></exception>
		public static async Task UpdateLeaderboardAsync(IServiceProvider serviceProvider, bool skipLastMessage = true)
		{
			var config = (Config) serviceProvider.GetService(typeof(Config));
			await UpdateLeaderboardAsync(config.CurrentEvent, serviceProvider, skipLastMessage);
		}

		public static async Task UpdateLeaderboardAsync(string eventName, IServiceProvider serviceProvider, bool skipLastMessage = true)
		{
			await Program.Log(new LogMessage(LogSeverity.Info, null, "Updating leaderboard"));
			var config = (Config) serviceProvider.GetService(typeof(Config));
			var client = (IDiscordClient) serviceProvider.GetService(typeof(IDiscordClient));
			
			var channel = await client.GetChannelAsync(config.BbegChannelId);
			if (channel is not SocketTextChannel bbegChannel) 
				throw new Exception("Bbeg channel is null!");
			
			var leaderboardFile = await LeaderboardParser.LoadLeaderboardAsync(eventName, serviceProvider) ?? 
			                      new LeaderboardFileData { Leaderboard = new Leaderboard(eventName, serviceProvider)};
			
			//Going through each message
			var messages = await ChannelUtils.GetMessages(serviceProvider, config.BumpChannelId);
			if (skipLastMessage) messages = messages.Skip(1).ToList();
			foreach (var channelMessage in messages)
			{
				//Updating the leaderboard if this message is the bump command 
				if (channelMessage.Author.Id == config.BumpBotId && 
				    channelMessage.Interaction != null && 
				    string.CompareOrdinal(channelMessage.Interaction.Name, config.BumpCommandString) == 0)
				{
					ulong userId = channelMessage.Interaction.User.Id;
					leaderboardFile.Leaderboard.UpdateUser(userId, 1);
				}

				//Deleting the message
				await Program.Log(new LogMessage(LogSeverity.Verbose, null, $"Deleting message with id {channelMessage.Id}"));
				await channelMessage.DeleteAsync();
			}
			
			//Creating a new leaderboard message if a message doesn't exist
			ulong messageId = 0;
			if (leaderboardFile.MessageId is null or 0)
			{
				string leaderboardMessage = await leaderboardFile.Leaderboard.ToStringWithUsernamesAsync();
				if (string.IsNullOrWhiteSpace(leaderboardMessage) == false)
				{
					var message = await bbegChannel.SendMessageAsync(leaderboardMessage);
					messageId = message.Id;
				}
			}
					
			//Updating the leaderboard message if the message exists
			else
			{
				var message = await bbegChannel.GetMessageAsync((ulong) leaderboardFile.MessageId) as RestUserMessage;
				if (message == null) throw new Exception("Error converting discord message to SocketUserMessage type");
				string newContent = await leaderboardFile.Leaderboard.ToStringWithUsernamesAsync();
				await message.ModifyAsync(m => m.Content = newContent);
				messageId = (ulong) leaderboardFile.MessageId;
			}
				
			//Writing changes to the file
			await LeaderboardParser.WriteLeaderboardAsync(config.CurrentEvent, leaderboardFile.Leaderboard, messageId);
			
			await Program.Log(new LogMessage(LogSeverity.Info, null, "Leaderboard updated"));
		}
	}
}