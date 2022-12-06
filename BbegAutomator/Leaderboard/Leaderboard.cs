using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BbegAutomator.Exceptions;
using Discord;

namespace BbegAutomator.Leaderboard;

public class Leaderboard
{
	private readonly IServiceProvider _serviceProvider;
	private readonly List<LeaderboardRecord> _records = new List<LeaderboardRecord>();
	private IEnumerable<LeaderboardRecord> Records => _records;
	private readonly string _name;

	public Leaderboard(string name, IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_name = name;
	}

	/// <summary>
	/// Adds <exception cref="pointsToAdd"> number of points to the user with the specified id</exception>
	/// </summary>
	/// <param name="id"></param>
	/// <param name="pointsToAdd"></param>
	public void UpdateUser(ulong id, int pointsToAdd)
	{
		var user = Records.FirstOrDefault(r => r.Id == id);
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

	/// <summary>
	/// Converts the leaderboard to string using users' usernames'
	/// </summary>
	/// <returns></returns>
	/// <exception cref="DependencyInjectionNullException"></exception>
	public async Task<string> ToStringWithUsernames()
	{
		var client = (IDiscordClient)_serviceProvider.GetService(typeof(IDiscordClient));
		if (client == null) throw new DependencyInjectionNullException();

		var builder = new StringBuilder(1024);
		builder.AppendLine($"Leaderboard for event \"{_name}\":");
		foreach (var r in _records)
		{
			var user = await client.GetUserAsync(r.Id);
			builder.AppendLine($"{user.Mention} {r.Points}");
		}

		return builder.ToString();
	}
}