using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Common.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if !NETSTANDARD1_1
using System.Net.WebSockets;
using Microsoft.Extensions.Options;
#endif

namespace EdjCase.JsonRpc.Client
{
	internal interface IRpcTransportClient
	{
		Task<Stream> SendRequestAsync(Uri uri, Stream requestStream, CancellationToken cancellationToken = default);
	}

	internal static class RpcTransportExtensions
	{
		public static async Task<string> SendRequestAsync(this IRpcTransportClient client, Uri uri, string requestJson, CancellationToken cancellationToken = default)
		{
			using (var requestStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson)))
			{
				using (Stream responseStream = await client.SendRequestAsync(uri, requestStream, cancellationToken))
				{
					using (StreamReader streamReader = new StreamReader(responseStream))
					{
						return await streamReader.ReadToEndAsync();
					}
				}
			}
		}
	}

	internal class HttpRpcTransportClient : IRpcTransportClient
	{
		/// <summary>
		/// Request encoding type for json content. If null, will default to UTF-8 
		/// </summary>
		public Encoding Encoding { get; }
		/// <summary>
		/// Request content type for json content. If null, will default to application/json
		/// </summary>
		public string ContentType { get; }
		/// <summary>
		/// Add headers to the underlying http client
		/// </summary>
		public IReadOnlyList<(string, string)> Headers { get; }

		private IStreamCompressor streamCompressor { get; }

		/// <summary>
		/// Factory to create authentication header for each request
		/// </summary>
		private IHttpAuthHeaderFactory? httpAuthHeaderFactory { get; }

		private IHttpClientFactory httpClientFactory { get; set; }


		public HttpRpcTransportClient(
			IStreamCompressor streamCompressor,
			IHttpClientFactory httpClientFactory,
			Encoding? encoding = null,
			string? contentType = null,
			IEnumerable<(string, string)>? headers = null,
			IHttpAuthHeaderFactory? httpAuthHeaderFactory = null)
		{
			this.Encoding = encoding ?? Defaults.Encoding;
			this.ContentType = contentType ?? Defaults.ContentType;
			this.Headers = headers?.ToList() ?? Defaults.GetHeaders();
			this.streamCompressor = streamCompressor ?? throw new ArgumentNullException(nameof(streamCompressor));
			this.httpAuthHeaderFactory = httpAuthHeaderFactory;
			this.httpClientFactory = httpClientFactory;
		}

		public async Task<Stream> SendRequestAsync(Uri uri, Stream requestStream, CancellationToken cancellationToken = default)
		{
			HttpClient httpClient = this.httpClientFactory.CreateClient();

			HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
			if (requestMessage.Headers.Any())
			{
				foreach ((string key, string value) in this.Headers)
				{
					requestMessage.Headers.Add(key, value);
				}
			}
			if (this.httpAuthHeaderFactory != null)
			{
				requestMessage.Headers.Authorization = await this.httpAuthHeaderFactory.CreateAuthHeader();
			}
			using (StreamReader streamReader = new StreamReader(requestStream))
			{
				string json = await streamReader.ReadToEndAsync();
				requestMessage.Content = new StringContent(json, this.Encoding, this.ContentType);
			}
			HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

			Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync();
			httpResponseMessage.Content.Headers.TryGetValues("Content-Encoding", out var encodings);

			//handle compressions
			if (encodings != null && encodings.Any())
			{
				foreach (string encoding in encodings)
				{
					if (this.streamCompressor.TryGetCompressionStream(responseStream, encoding, CompressionMode.Decompress, out Stream decompressedResponseStream))
					{
						responseStream = decompressedResponseStream;
						break;
					}
				}
			}
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				using (StreamReader streamReader = new StreamReader(responseStream))
				{
					string content = await streamReader.ReadToEndAsync();
					throw new RpcClientInvalidStatusCodeException(httpResponseMessage.StatusCode, content);
				}
			}

			// uncompressed, standard response
			return responseStream;
		}
	}
}
