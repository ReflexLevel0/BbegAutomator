using System;

namespace BbegAutomator.Exceptions
{
	public class DependencyInjectionNullException : Exception
	{
		public DependencyInjectionNullException() : base("Dependency injection returned a null object!")
		{
			
		}
	}
}