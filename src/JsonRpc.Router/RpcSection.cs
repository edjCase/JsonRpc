using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JsonRpc.Router
{
	public class RpcRoute
	{
		public string Name { get; }
		private List<Type> types { get; } = new List<Type>();

		public RpcRoute(string name = null)
		{
			this.Name = name;
		}

		public bool AddClass<T>()
		{
			Type type = typeof (T);
			if (this.types.Any(t => t == type))
			{
				return false;
			}
			this.types.Add(type);
			return true;
		}

		public List<Type> GetClasses()
		{
			return new List<Type>(this.types);
		} 
	}

	public class RpcRouteCollection : ICollection<RpcRoute>
	{
		private List<RpcRoute> routeList { get; } = new List<RpcRoute>();
		
		public int Count => this.routeList.Count;

		public bool IsReadOnly => false;
		
		public RpcRoute GetByName(string routeName)
		{
			if (string.IsNullOrWhiteSpace(routeName))
			{
				return this.routeList.FirstOrDefault(s => string.IsNullOrWhiteSpace(s.Name));
			}
			return this.routeList.FirstOrDefault(s => string.Equals(s.Name, routeName, StringComparison.OrdinalIgnoreCase));
		}

		public void Add(RpcRoute route)
		{
			if(route == null)
			{
				throw new ArgumentNullException(nameof(route));
			}
			RpcRoute duplicateRoute = this.GetByName(route.Name);
			if(duplicateRoute != null)
			{
				throw new ArgumentException($"Route with the name '{route.Name}' already exists");
			}
			this.routeList.Add(route);
		} 

		public void Clear()
		{
			this.routeList.Clear();
		}

		public bool Contains(RpcRoute route)
		{
			return this.routeList.Contains(route);
		}

		public void CopyTo(RpcRoute[] array, int arrayIndex)
		{
			this.routeList.CopyTo(array, arrayIndex);
		}

		public bool Remove(RpcRoute route)
		{
			return this.routeList.Remove(route);
		}

		public IEnumerator<RpcRoute> GetEnumerator()
		{
			return this.routeList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.routeList.GetEnumerator();
		}
	}
}
