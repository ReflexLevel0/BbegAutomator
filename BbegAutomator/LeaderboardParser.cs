using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;

namespace BbegAutomator
{
	public static class LeaderboardParser
	{
		public static LeaderboardRecord ParseLine(string line)
		{
			string[] parts = line.Split(" ");
			return new LeaderboardRecord(Convert.ToUInt64(parts[0]), Convert.ToInt32(parts[1]));
		}
		
		/// <summary>
		/// Returns all leaderboards
		/// </summary>
		/// <param name="serviceProvider"></param>
		/// <returns></returns>
		public static async Task<List<LeaderboardFileData>> LoadAllLeaderboardsAsync(IServiceProvider serviceProvider)
		{
			var data = new List<LeaderboardFileData>();
			foreach(string eventName in EventUtils.GetEventNames())
			{
				data.Add(await LoadLeaderboardAsync(eventName, serviceProvider));
			}
			return data;
		}

		/// <summary>
		/// Parses the leaderboard file from the specified year and month 
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="serviceProvider"></param>
		/// <returns>Leaderboard with data from the specified year and month (or null if file doesn't exist)</returns>
		public static async Task<LeaderboardFileData> LoadLeaderboardAsync(string eventName, IServiceProvider serviceProvider)
		{
			if (string.IsNullOrWhiteSpace(eventName)) throw new EventNameNullException();
			string filePath = FileUtils.EventNameToFilePath(eventName);
			if (!File.Exists(filePath)) return null;

			var fileData = new LeaderboardFileData {Leaderboard = new Leaderboard(eventName, serviceProvider)};
			string[] lines = await File.ReadAllLinesAsync(filePath);
			if (lines.Length == 0) return fileData;
			
			bool firstLine = true;
			foreach(string line in lines)
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
		/// Creates a new leaderboard file and writes <exception cref="leaderboard"> data to it</exception>
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="leaderboard"></param>
		/// <param name="messageId"></param>
		public static async Task WriteLeaderboardAsync(string eventName, Leaderboard leaderboard, ulong messageId)
		{
			if (string.IsNullOrWhiteSpace(eventName)) throw new EventNameNullException();
			string filePath = FileUtils.EventNameToFilePath(eventName);
			await Program.Log(new LogMessage(LogSeverity.Verbose, null, $"Writing data to {filePath}"));
			
			var builder = new StringBuilder(1024);
			builder.AppendLine(messageId.ToString());
			builder.Append(leaderboard);
			
			Directory.CreateDirectory("data");
			await File.WriteAllTextAsync(filePath, builder.ToString());
		}
	}
}