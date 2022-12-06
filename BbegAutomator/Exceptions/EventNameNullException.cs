using System;

namespace BbegAutomator.Exceptions;

public class EventNameNullException : Exception
{
	public EventNameNullException() : base("Event name can't be null/white space!")
	{
		
	}
}