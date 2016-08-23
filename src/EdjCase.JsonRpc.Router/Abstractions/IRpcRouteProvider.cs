using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	/// <summary>
	/// Interface to provide the middleware with available routes and their criteria
	/// </summary>
	public interface IRpcRouteProvider
	{
#if !NETSTANDARD1_3
		bool AutoDetectControllers { get; set; }
#endif
		List<RpcRoute> GetRoutes();
		void RegisterRoute(IEnumerable<RouteCriteria> criteria, string name = null);
	}

	/// <summary>
	/// Criteria that has to be met for the specified route to match
	/// </summary>
	public class RouteCriteria
	{
		/// <summary>
		/// List of types to match against
		/// </summary>
		public IReadOnlyList<Type> Types { get; }

		/// <param name="types">List of types to match against</param>
		public RouteCriteria(List<Type> types)
		{
			if(types == null || types.Any())
			{
				throw new ArgumentException("At least one type must be specified.", nameof(types));
			}
			this.Types = types;
		}

		/// <param name="type">Type to match against</param>
		public RouteCriteria(Type type, IEnumerable<IAuthorizeData> authorizeData = null)
		{
			if(type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			this.Types = new List<Type> { type };
		}
	}

	public static class RouteProviderExtensions
	{
		public static void RegisterRoute(this IRpcRouteProvider routeProvider, RouteCriteria criteria, string name = null)
		{
			routeProvider.RegisterRoute(new List<RouteCriteria> { criteria }, name);
		}

		public static void RegisterTypeRoute<T>(this IRpcRouteProvider routeProvider, string name = null)
		{
			routeProvider.RegisterRoute(new List<RouteCriteria> { new RouteCriteria(typeof(T)) }, name);
		}
	}
}
