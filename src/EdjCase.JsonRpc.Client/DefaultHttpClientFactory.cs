using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Common.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Client
{
#if NETSTANDARD1_1
	public interface IHttpClientFactory
	{
		HttpClient CreateClient(string name);
	}
	public static class HttpClientFactoryExtensions
	{		
		public static HttpClient CreateClient(this IHttpClientFactory factory)
		{
			return factory.CreateClient("Default");
		}
	}
#endif

	public class DefaultHttpClientFactory : IHttpClientFactory, IDisposable
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