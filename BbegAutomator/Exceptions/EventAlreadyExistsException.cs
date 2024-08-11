namespace BbegAutomator.Exceptions;

public class EventAlreadyExistsException(string name) : Exception($"Failed to create an event. Event with name {name} already exists!")
{
}