using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace EdjCase.JsonRpc.Client
{

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