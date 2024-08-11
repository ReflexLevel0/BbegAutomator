using System.Text;
using BbegAutomator.Exceptions;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace BbegAutomator.Leaderboard;

public class Leaderboard(IServiceProvider serviceProvider) : ILeaderboard
{
	private IList<LeaderboardRecord> _records = new List<LeaderboardRecord>();

	public string EventName { get; set; }
	
	public void UpdateUser(ulong id, int pointsToAdd)
	{
		var user = _records.FirstOrDefault(r => r.Id == id);
		if (user == null) _records.Add(new LeaderboardRecord(id, pointsToAdd));
		else user.Points += pointsToAdd;
	}

	/// <summary>
	/// Converts the leaderboard to string using users' ids'
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		var builder = new StringBuilder(1024);
		foreach (var r in _records)
		{
			builder.AppendLine($"{r.Id} {r.Points}");
		}

		return builder.ToString();
	}

	public async Task<string> ToStringWithUsernames()
	{
		var client = serviceProvider.GetRequiredService<IDiscordClient>();
		var builder = new StringBuilder(1024);
		builder.AppendLine($"Leaderboard for event \"{EventName}\":");
		foreach (var r in _records)
		{
			var user = await client.GetUserAsync(r.Id);
			builder.AppendLine($"{user.Mention} {r.Points}");
		}

		return builder.ToString();
	}

	public bool IsEmpty() => _records.Count == 0;
}