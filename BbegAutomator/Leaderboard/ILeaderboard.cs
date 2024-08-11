namespace BbegAutomator.Leaderboard;

public interface ILeaderboard
{
	string EventName { get; set; }
	
    /// <summary>
	/// Adds <exception cref="pointsToAdd"> number of points to the user with the specified id</exception>
	/// </summary>
	/// <param name="id"></param>
	/// <param name="pointsToAdd"></param>
    void UpdateUser(ulong id, int pointsToAdd);

    /// <summary>
	/// Converts the leaderboard to string using users' usernames'
	/// </summary>
	/// <returns></returns>
    Task<string> ToStringWithUsernames();

    bool IsEmpty();
}