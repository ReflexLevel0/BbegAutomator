using BbegAutomator.Exceptions;

namespace BbegAutomator.Leaderboard;

public interface ILeaderboardUtils
{
    /// <summary>
    /// Returns all leaderboards
    /// </summary>
    /// <returns></returns>
    Task<List<LeaderboardFileData>> LoadAllLeaderboards();

    /// <summary>
    /// Parses the leaderboard file for the specified event 
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns>Leaderboard with data from the specified year and month (or null if file doesn't exist)</returns>
    /// <exception cref="EventDoesntExistException"></exception>
    Task<LeaderboardFileData> LoadLeaderboard(string eventName);

    /// <summary>
    /// Reads all messages in the bump channel and updates the leaderboard for the current event
    /// </summary>
    /// <param name="skipLastMessage">Skips the last bump command in the channel if true</param>
    /// <exception cref="EventDoesntExistException"></exception>
    Task UpdateLeaderboard(bool skipLastMessage = true);

    /// <summary>
    /// Reads all messages in the bump channel and updates the leaderboard for the specified event
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="skipLastMessage"></param>
    /// <exception cref="EventDoesntExistException"></exception>
    Task UpdateLeaderboard(string eventName, bool skipLastMessage = true);
}