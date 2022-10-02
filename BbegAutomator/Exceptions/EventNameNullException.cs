using System;

namespace BbegAutomator.Exceptions;

public class EventNameNullException : Exception
{
	public EventNameNullException() : base("Event name shouldn't be null!")
	{
		
	}
}