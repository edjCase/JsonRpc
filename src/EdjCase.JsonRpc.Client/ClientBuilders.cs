
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common.Tools;
using Newtonsoft.Json;

namespace EdjCase.JsonRpc.Client
{
	public class HttpRpcClientBuilder : RpcClientBuilder
	{
		private IStreamCompressor streamCompressor { get; set; }
		private IHttpAuthHeaderFactory httpAuthHeaderFactory { get; set; }
		private HttpOptions options { get; } = new HttpOptions();


		public HttpRpcClientBuilder(Uri baseUrl)
		: base(baseUrl)
		{
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

		public HttpRpcClientBuilder UsingAuthHeaderFactory(Func<Task<AuthenticationHeaderValue>> authHeaderFactory)
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

		public HttpRpcClientBuilder UsingStreamCompressor(IStreamCompressor streamCompressor)
		{
			if (this.streamCompressor != null)
			{
				throw new InvalidOperationException("Stream compressor has already been configured.");
			}
			this.streamCompressor = streamCompressor;
			return this;
		}

		public new HttpRpcClientBuilder ConfigureEvents(Action<RpcEvents> configure)
		{
			return (HttpRpcClientBuilder)base.ConfigureEvents(configure);
		}

		public new HttpRpcClientBuilder UsingRequestSerializer(IRequestSerializer requestSerializer)
		{
			return (HttpRpcClientBuilder)base.UsingRequestSerializer(requestSerializer);
		}

		public HttpRpcClientBuilder UsingDefaultJsonSerializer(JsonSerializerSettings settings = null, IErrorDataSerializer errorDataSerializer = null)
		{
			return (HttpRpcClientBuilder)base.UsingRequestSerializer(new DefaultRequestJsonSerializer(jsonSerializerSettings: settings, errorDataSerializer: errorDataSerializer));
		}

		public override RpcClient Build()
		{
			var requestSerializer = this.RequestSerializer ?? new DefaultRequestJsonSerializer();
			var transportClient = new HttpRpcTransportClient(
				encoding: this.options.Encoding,
				contentType: this.options.ContentType,
				headers: this.options.Headers,
				streamCompressor: this.streamCompressor,
				httpAuthHeaderFactory: this.httpAuthHeaderFactory);
			return new RpcClient(this.BaseUrl, requestSerializer, transportClient, this.Events);
		}

		public class HttpOptions
		{
			public string ContentType { get; set; } = Defaults.ContentType;
			public Encoding Encoding { get; set; } = Defaults.Encoding;
			public List<(string, string)> Headers { get; set; } = Defaults.GetHeaders();
		}
	}

	public abstract class RpcClientBuilder
	{
		protected Uri BaseUrl { get; }
		protected IRequestSerializer RequestSerializer { get; set; }
		protected RpcEvents Events { get; } = new RpcEvents();

		public RpcClientBuilder(Uri baseUrl)
		{
			this.BaseUrl = baseUrl;
		}

		public RpcClientBuilder ConfigureEvents(Action<RpcEvents> configure)
		{
			configure(this.Events);
			return this;
		}

		public RpcClientBuilder UsingRequestSerializer(IRequestSerializer requestSerializer)
		{
			if (requestSerializer == null)
			{
				throw new ArgumentNullException(nameof(requestSerializer));
			}
			if (this.RequestSerializer != null)
			{
				throw new InvalidOperationException("Request serializer has already been configured.");
			}
			this.RequestSerializer = requestSerializer;
			return this;
		}

		public abstract RpcClient Build();
	}
}