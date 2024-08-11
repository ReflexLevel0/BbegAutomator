using BbegAutomator.Exceptions;

namespace BbegAutomator.Utils;

public interface IEventUtils
{
    /// <summary>
    /// Creates a new event with the specified name
    /// </summary>
    /// <param name="eventName">Name of the event to be created</param>
    /// <returns></returns>
    /// <exception cref="EventAlreadyExistsException">Thrown if an event with the same event as <paramref name="eventName"/> already exists</exception>
    /// <exception cref="EventNameNullException">Thrown if <see cref="eventName"/> is null or white space</exception>
    Task<IConfig> CreateEvent(string eventName);

    /// <summary>
    /// Renames the specified event to the new name
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="newEventName"></param>
    /// <returns></returns>
    /// <exception cref="EventDoesntExistException">Thrown if event <paramref name="eventName"/> doesn't exist</exception>
    Task<IConfig> RenameEvent(string eventName, string newEventName);

    /// <summary>
    /// Deletes the specified event
    /// </summary>
    /// <param name="eventName"></param>
    /// <exception cref="EventDoesntExistException"></exception>
    Task DeleteEvent(string eventName);

    IEnumerable<string> GetEventNames();

    /// <summary>
    /// Converts <paramref name="eventName"/> to a path to the file in which the leaderboard for the event is stored
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    string EventNameToFilePath(string eventName);

    bool EventExists(string eventName);
}