using System;
using System.Collections.Generic;
using Discord.Rest;

namespace BbegAutomator
{
	public static class BbegLeaderboardParser
	{
		public static BbegLeaderboard ParseLeaderboardMessage(RestMessage leaderboardMessage)
		{
			var leaderboard = new BbegLeaderboard();

			//Splitting the message into lines and going through each line
			string[] lines = leaderboardMessage.Content.Split("\n");
			bool leaderboardFound = false;
			foreach (string line in lines)
			{
				switch (leaderboardFound)
				{
					//If the line where the leaderboard starts has been found
					case false when line.StartsWith("Damage numbers"):
						leaderboardFound = true;
						break;
					
					//Parsing the line containing a username and points for that user
					case true:
						var record = BbegLeaderboardRecord.ParseLine(line);
						leaderboard.UpdateUser(record.Username, record.Points);
						break;
				}
			}
			
			if (!leaderboardFound)
			{
				throw new Exception($"Failed to parse bbeg leaderboard. Leaderboard content: {leaderboardMessage.Content}");
			}

			return leaderboard;
		}
	}
}