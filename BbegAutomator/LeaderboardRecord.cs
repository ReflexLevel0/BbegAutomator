using System;

namespace BbegAutomator
{
	public class BbegLeaderboardRecord
	{
		public ulong Id { get; }
		public int Points { get; set; }

		public BbegLeaderboardRecord(ulong id, int points)
		{
			Id = id;
			Points = points;
		}
	}
}