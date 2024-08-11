namespace BbegAutomator.Leaderboard;

public class LeaderboardRecord(ulong id, int points)
{
	public ulong Id => id;
	public int Points { get; set; } = points;
}