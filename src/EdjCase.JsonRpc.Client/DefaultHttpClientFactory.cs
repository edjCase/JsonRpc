using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace EdjCase.JsonRpc.Client
{
#if NETSTANDARD1_1
	internal interface IHttpClientFactory
	{
		HttpClient CreateClient(string name);
	}
	internal static class HttpClientFactoryExtensions
	{		
		public static HttpClient CreateClient(this IHttpClientFactory factory)
		{
			return factory.CreateClient("Default");
		}
	}
#endif

	internal class DefaultHttpClientFactory : IHttpClientFactory, IDisposable
	{
		public ConcurrentDictionary<string, HttpClient> Clients { get; } = new ConcurrentDictionary<string, HttpClient>();

		public HttpMessageHandler? MessageHandler { get; }
		public DefaultHttpClientFactory(HttpMessageHandler? handler = null)
		{
			this.MessageHandler = handler;
		}

		public HttpClient CreateClient(string name)
		{
			return this.Clients.GetOrAdd(name, BuildClient);

			HttpClient BuildClient(string n)
			{
				return this.MessageHandler == null ? new HttpClient() : new HttpClient(this.MessageHandler);
			}
		}

		public void Dispose()
		{
			foreach (HttpClient client in this.Clients.Values)
			{
				client?.Dispose();
			}
		}
	}
}