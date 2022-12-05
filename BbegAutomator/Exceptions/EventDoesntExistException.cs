using System;

namespace BbegAutomator.Exceptions;

public class EventDoesntExistException : Exception
{
	public EventDoesntExistException(string eventName) : base($"Event \"{eventName}\" doesn't exist!")
	{
		
	}
}