using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Common.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Client
{
	public class HttpRpcClientBuilder
	{
		private IHttpAuthHeaderFactory? httpAuthHeaderFactory { get; set; }
		private HttpOptions options { get; } = new HttpOptions();

		private Uri BaseUrl { get; }
		private RpcEvents Events { get; } = new RpcEvents();
		private JsonSerializerSettings jsonSerializerSettings { get; } = new JsonSerializerSettings();
		private Dictionary<int, Type> errorTypes { get; } = new Dictionary<int, Type>();



		public HttpRpcClientBuilder(Uri baseUrl)
		{
			this.BaseUrl = baseUrl;
		}

		public HttpRpcClientBuilder ConfigureHttp(Action<HttpOptions> configure)
		{
			configure(this.options);
			return this;
		}
		public HttpRpcClientBuilder UsingAuthHeader(AuthenticationHeaderValue authHeader)
		{
			if (authHeader == null)
			{
				throw new ArgumentNullException(nameof(authHeader));
			}
			if (this.httpAuthHeaderFactory != null)
			{
				throw new InvalidOperationException("Auth header factory has already been configured.");
			}
			this.httpAuthHeaderFactory = new DefaultHttpAuthHeaderFactory(authHeader);
			return this;
		}

		public HttpRpcClientBuilder UsingAuthHeaderFactory(IHttpAuthHeaderFactory factory)
		{
			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}
			if (this.httpAuthHeaderFactory != null)
			{
				throw new InvalidOperationException("Auth header factory has already been configured.");
			}
			this.httpAuthHeaderFactory = factory;
			return this;
		}

		public HttpRpcClientBuilder UsingAuthHeaderFactory(Func<Task<AuthenticationHeaderValue?>> authHeaderFactory)
		{
			if (authHeaderFactory == null)
			{
				throw new ArgumentNullException(nameof(authHeaderFactory));
			}
			if (this.httpAuthHeaderFactory != null)
			{
				throw new InvalidOperationException("Auth header factory has already been configured.");
			}
			this.httpAuthHeaderFactory = new DefaultHttpAuthHeaderFactory(authHeaderFactory);
			return this;
		}

		public HttpRpcClientBuilder ConfigureEvents(Action<RpcEvents> configure)
		{
			configure(this.Events);
			return this;
		}

		public HttpRpcClientBuilder ConfigureSerializerSettings(Action<JsonSerializerSettings> configure)
		{
			configure(this.jsonSerializerSettings);
			return this;
		}

		public HttpRpcClientBuilder DeserializeErrorDataAs<T>(RpcErrorCode errorCode)
		{
			return this.DeserializeErrorDataAs<T>((int)errorCode);
		}
		public HttpRpcClientBuilder DeserializeErrorDataAs<T>(int errorCode)
		{
			return this.DeserializeErrorDataAs(errorCode, typeof(T));
		}
		public HttpRpcClientBuilder DeserializeErrorDataAs(int errorCode, Type type)
		{
			this.errorTypes.Add(errorCode, type);
			return this;
		}


		public RpcClient Build()
		{
			var streamCompressor = new DefaultStreamCompressor();
			var httpClientFactory = new DefaultHttpClientFactory();
			var transportClient = new HttpRpcTransportClient(
				streamCompressor,
				httpClientFactory,
				encoding: this.options.Encoding,
				contentType: this.options.ContentType,
				headers: this.options.Headers,
				httpAuthHeaderFactory: this.httpAuthHeaderFactory);
			var requestSerializer = new DefaultRequestJsonSerializer(this.jsonSerializerSettings);
			return new RpcClient(this.BaseUrl, transportClient, requestSerializer, this.Events);
		}

		public class HttpOptions
		{
			public string ContentType { get; set; } = Defaults.ContentType;
			public Encoding Encoding { get; set; } = Defaults.Encoding;
			public List<(string, string)> Headers { get; set; } = Defaults.GetHeaders();
		}
	}

}
