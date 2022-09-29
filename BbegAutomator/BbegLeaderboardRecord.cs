using System;

namespace BbegAutomator
{
	public class BbegLeaderboardRecord
	{
		public string Username { get; }
		public int Points { get; set; }

		public BbegLeaderboardRecord(string username, int points)
		{
			Username = username;
			Points = points;
		}

		public static BbegLeaderboardRecord ParseLine(string line)
		{
			string[] parts = line.Split(" ");
			return new BbegLeaderboardRecord(parts[0], Convert.ToInt32(parts[1]));
		}
	}
}