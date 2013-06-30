using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FakeProject
{
	/// <summary>
	/// Builder that will generate inheritor for specified assembly and will overload specified internal virtual method 
	/// </summary>
	/// <typeparam name="TFakeType">Target type</typeparam>
	public class FakeBuilder<TFakeType>
	{
		private readonly Action _callback;

		private readonly Type _targetType;

		private readonly string _targetMethodName;

		private readonly string _slotName;

		private readonly bool _callBaseMethod;

		public FakeBuilder(Action callback, string targetMethodName, bool callBaseMethod)
		{
			int randomId = new Random((int)DateTime.Now.Ticks).Next();
			_slotName = string.Format("FakeSlot_{0}", randomId);
			_callback = callback;
			_targetType = typeof(TFakeType);
			_targetMethodName = targetMethodName;
			_callBaseMethod = callBaseMethod;
		}

		public TFakeType CreateFake()
		{
			// as CorpLibrary1 can't use code from unreferences assemblies, we need to store this Action somewhere. 
			// And Thread is not bad place for that. It's not the best place as it won't work in multithread application, but it's just a sample
			LocalDataStoreSlot slot = Thread.AllocateNamedDataSlot(_slotName);
			Thread.SetData(slot, _callback);

			// then we generate new assembly with the same nameand public key as target assembly trusts by InternalsVisibleTo attribute
			var newTypeName = _targetType.Name + "Fake";
			var targetAssembly = Assembly.GetAssembly(_targetType);

			AssemblyName an = new AssemblyName();
			an.Name = GetFakeAssemblyName(targetAssembly);

			// copying public key to new generated assembly
			var assemblyName = targetAssembly.GetName();
			an.SetPublicKey(assemblyName.GetPublicKey());
			an.SetPublicKeyToken(assemblyName.GetPublicKeyToken());

			AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, true);

			// create inheritor for specified type
			TypeBuilder typeBuilder = moduleBuilder.DefineType(newTypeName, TypeAttributes.Public | TypeAttributes.Class, _targetType);

			// LambdaExpression.CompileToMethod can be used only with static methods, so we need to create another method that will call our Inject method
			// we can do the same via ILGenerator, but expression trees are more easy to use
			MethodInfo methodInfo = CreateMethodInfo(moduleBuilder);

			MethodBuilder methodBuilder = typeBuilder.DefineMethod(_targetMethodName, MethodAttributes.Public | MethodAttributes.Virtual);

			ILGenerator ilGenerator = methodBuilder.GetILGenerator();

			// call our static method that will call inject method
			ilGenerator.EmitCall(OpCodes.Call, methodInfo, null);

			// in case if we need, then we put call to base method
			if (_callBaseMethod)
			{
				var baseMethodInfo = _targetType.GetMethod(_targetMethodName, BindingFlags.NonPublic | BindingFlags.Instance);

				// place this to stack
				ilGenerator.Emit(OpCodes.Ldarg_0);

				// call the base method
				ilGenerator.EmitCall(OpCodes.Call, baseMethodInfo, new Type[0]);

				// return
				ilGenerator.Emit(OpCodes.Ret);
			}

			// generate type, create it and return to caller
			Type cheatType = typeBuilder.CreateType();
			object type = Activator.CreateInstance(cheatType);

			return (TFakeType)type;
		}
	
		/// <summary>
		/// Get name of assembly from InternalsVisibleTo AssemblyName
		/// </summary>
		private static string GetFakeAssemblyName(Assembly assembly)
		{
			var internalsVisibleAttr = assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), true).FirstOrDefault() as InternalsVisibleToAttribute;

			if (internalsVisibleAttr == null)
			{
				throw new InvalidOperationException("Assembly hasn't InternalVisibleTo attribute");
			}

			var ind = internalsVisibleAttr.AssemblyName.IndexOf(",");
			var name = internalsVisibleAttr.AssemblyName.Substring(0, ind);

			return name;
		}

		/// <summary>
		/// Generate such code:
		/// ((Action)Thread.GetData(Thread.GetNamedDataSlot(_slotName))).Invoke();
		/// </summary>
		private LambdaExpression MakeStaticExpressionMethod()
		{
			var allocateMethod = typeof(Thread).GetMethod("GetNamedDataSlot", BindingFlags.Static | BindingFlags.Public);
			var getDataMethod = typeof(Thread).GetMethod("GetData", BindingFlags.Static | BindingFlags.Public);

			var call = Expression.Call(allocateMethod, Expression.Constant(_slotName));
			var getCall = Expression.Call(getDataMethod, call);
			var convCall = Expression.Convert(getCall, typeof(Action));

			var invokExpr = Expression.Invoke(convCall);

			var lambda = Expression.Lambda<Action>(invokExpr);

			return lambda;
		}


		/// <summary>
		/// Generate static class with one static function that will execute Action from Thread NamedDataSlot
		/// </summary>
		private MethodInfo CreateMethodInfo(ModuleBuilder moduleBuilder)
		{
			var methodName = "_StaticTestMethod_" + _slotName;
			var className = "_StaticClass_" + _slotName;

			TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodName, MethodAttributes.Static | MethodAttributes.Public);
			LambdaExpression expression = MakeStaticExpressionMethod();

			expression.CompileToMethod(methodBuilder);

			var type = typeBuilder.CreateType();

			return type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
		}
	}
}