namespace BbegAutomator
{
	public class LeaderboardRecord
	{
		public ulong Id { get; }
		public int Points { get; set; }

		public LeaderboardRecord(ulong id, int points)
		{
			Id = id;
			Points = points;
		}
	}
}