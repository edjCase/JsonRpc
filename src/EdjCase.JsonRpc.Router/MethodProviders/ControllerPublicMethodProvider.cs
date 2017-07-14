using EdjCase.JsonRpc.Router.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace EdjCase.JsonRpc.Router.Criteria
{
	/// <summary>
	/// Criteria that has to be met for the specified route to match
	/// </summary>
	public class ControllerPublicMethodProvider : IRpcMethodProvider
	{
		/// <summary>
		/// List of types to match against
		/// </summary>
		public IReadOnlyList<Type> Types { get; }

		private List<MethodInfo> methodCache { get; set; }

		/// <param name="types">List of types to match against</param>
		public ControllerPublicMethodProvider(List<Type> types)
		{
			if (types == null || !types.Any())
			{
				throw new ArgumentException("At least one type must be specified.", nameof(types));
			}
			this.Types = types;
		}

		/// <param name="type">Type to match against</param>
		public ControllerPublicMethodProvider(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			this.Types = new List<Type> { type };
		}
		
		public List<MethodInfo> GetRouteMethods()
		{
			if (this.methodCache == null)
			{
				var methods = new List<MethodInfo>();
				foreach (Type type in this.Types)
				{
					List<MethodInfo> publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
						//Ignore ToString, GetHashCode and Equals
						.Where(m => m.DeclaringType != typeof(object))
						.ToList();
					methods.AddRange(publicMethods);
				}
				this.methodCache = methods;
			}
			return this.methodCache;
		}
	}
}
