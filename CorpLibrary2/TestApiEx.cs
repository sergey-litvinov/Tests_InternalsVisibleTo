using System;

using CorpLibrary1;

namespace CorpLibrary2
{
    public class TestApiEx : TestApi
    {
	    public TestApiEx()
	    {		    
	    }

		internal override void DoSomething()
		{
			Console.WriteLine("Inherite DoSomething");
		}
    }
}
