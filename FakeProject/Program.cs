using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using CorpLibrary1;

namespace FakeProject
{
	class Program
	{
		static void Main(string[] args)
		{
			var builder = new FakeBuilder<TestApi>(InjectBadCode, "DoSomething", true);
			TestApi fakeType = builder.CreateFake();

			fakeType.DoApiTest();

			Console.ReadLine();
		}

		public static void InjectBadCode()
		{
			Console.WriteLine("Inject bad code");
		}
	}
}
