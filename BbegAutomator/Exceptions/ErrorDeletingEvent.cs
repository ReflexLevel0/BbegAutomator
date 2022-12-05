using System;

namespace BbegAutomator.Exceptions;

public class ErrorDeletingEvent : Exception
{
	public ErrorDeletingEvent() : base("Current event can't be deleted!")
	{
		
	}
}