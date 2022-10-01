using System;
using System.IO;

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
		public static BbegLeaderboard LoadFile(int year, int month)
		{
			string fileName = $"{year}-{(month < 10 ? "0" + month : month)}";
			string filePath = $"data/{fileName}";

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
	}
}