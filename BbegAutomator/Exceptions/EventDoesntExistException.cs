namespace BbegAutomator.Exceptions;

public class EventDoesntExistException(string eventName) : Exception($"Event \"{eventName}\" doesn't exist!")
{
}