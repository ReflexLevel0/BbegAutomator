using System;

namespace BbegAutomator
{
	public class DependencyInjectionNullException : Exception
	{
		public DependencyInjectionNullException() : base("Dependency injection returned a null object!")
		{
			
		}
	}
}