using System;

namespace CorpLibrary1
{
	public class TestApi
	{
		internal virtual void DoSomething()
		{
			Console.WriteLine("Base DoSomething");
		}

		public void DoApiTest()
		{
			// some internal logic
			// ...
			DoSomething();
		}
	}
}
