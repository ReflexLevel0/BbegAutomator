using System;

namespace BbegAutomator.Exceptions;

public class EventAlreadyExistsException : Exception
{
	public EventAlreadyExistsException(string name) : base($"Failed to create an event. Event with name {name} already exists!")
	{
		
	}
}