using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
		/// Parses the leaderboard file from the specified year and month 
		/// </summary>
		/// <param name="year"></param>
		/// <param name="month"></param>
		/// <param name="serviceProvider"></param>
		/// <returns>Leaderboard with data from the specified year and month (or null if file doesn't exist)</returns>
		public static async Task<LeaderboardFileData> LoadLeaderboardAsync(int year, int month, IServiceProvider serviceProvider)
		{
			string filePath = DateToFilePath(year, month);
			if (!File.Exists(filePath)) return null;

			var fileData = new LeaderboardFileData {Leaderboard = new Leaderboard(serviceProvider)};
			string[] lines = await File.ReadAllLinesAsync(filePath);
			
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
		/// <param name="year"></param>
		/// <param name="month"></param>
		/// <param name="leaderboard"></param>
		/// <param name="messageId"></param>
		public static async Task WriteLeaderboardAsync(int year, int month, Leaderboard leaderboard, ulong messageId)
		{
			string filePath = DateToFilePath(year, month);
			await Program.Log(new LogMessage(LogSeverity.Verbose, null, $"Writing data to {filePath}"));
			var builder = new StringBuilder(1024);
			builder.AppendLine(messageId.ToString());
			builder.Append(leaderboard);
			Directory.CreateDirectory("data");
			await File.WriteAllTextAsync(filePath, builder.ToString());
		}

		private static string DateToFilePath(int year, int month) => $"data/{year}-{(month < 10 ? "0" + month : month)}.txt";
	}
}