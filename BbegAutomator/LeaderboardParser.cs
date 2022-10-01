using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BbegAutomator
{
	public static class BbegLeaderboardParser
	{
		public static BbegLeaderboardRecord ParseLine(string line)
		{
			string[] parts = line.Split(" ");
			return new BbegLeaderboardRecord(Convert.ToUInt64(parts[0]), Convert.ToInt32(parts[1]));
		}
		
		/// <summary>
		/// Parses the leaderboard file from the specified year and month 
		/// </summary>
		/// <param name="year"></param>
		/// <param name="month"></param>
		/// <returns>Leaderboard with data from the specified year and month (or null if file doesn't exist)</returns>
		public static BbegLeaderboard LoadLeaderboard(int year, int month)
		{
			string filePath = DateToFilePath(year, month);
			if (!File.Exists(filePath)) return null; 
			
			var leaderboard = new BbegLeaderboard();
			string[] lines = File.ReadAllLines(filePath);
			foreach(string line in lines)
			{
				var record = ParseLine(line);
				leaderboard.UpdateUser(record.Id, record.Points);
			}

			return leaderboard;
		}

		/// <summary>
		/// Creates a new leaderboard file and writes <exception cref="leaderboard"> data to it</exception>
		/// </summary>
		/// <param name="year"></param>
		/// <param name="month"></param>
		/// <param name="leaderboard"></param>
		/// <param name="messageId"></param>
		public static async Task WriteLeaderboard(int year, int month, BbegLeaderboard leaderboard, ulong messageId)
		{
			string filePath = DateToFilePath(year, month);
			var builder = new StringBuilder(1024);
			builder.AppendLine(messageId.ToString());
			foreach(var record in leaderboard.Leaderboard)
			{
				builder.AppendLine($"{record.Id} {record.Points}");
			}
			await File.WriteAllTextAsync(filePath, builder.ToString());
		}

		private static string DateToFilePath(int year, int month) => $"data/{year}-{(month < 10 ? "0" + month : month)}";
	}
}