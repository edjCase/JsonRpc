using EdjCase.JsonRpc.Core;
using EdjCase.JsonRpc.Core.Tools;
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
	public interface IRpcTransportClient
	{
		Task<Stream> SendRequestAsync(Uri uri, Stream requestStream, CancellationToken cancellationToken = default);
	}

	public static class RpcTransportExtensions
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

	public class HttpRpcTransportClient : IRpcTransportClient
	{
		/// <summary>
		/// Authentication header value factory for the rpc request being sent. If the server requires
		/// authentication this requires a value. Otherwise it can be null
		/// </summary>
		public Func<Task<AuthenticationHeaderValue>> AuthHeaderValueFactory { get; }
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

		public HttpRpcTransportClient(Func<Task<AuthenticationHeaderValue>> authHeaderValueFactory = null,
			Encoding encoding = null,
			string contentType = null,
			IEnumerable<(string, string)> headers = null,
			IStreamCompressor streamCompressor = null)
		{
			this.AuthHeaderValueFactory = authHeaderValueFactory;
			this.Encoding = encoding ?? Encoding.UTF8;
			this.ContentType = contentType ?? "application/json";
			this.Headers = headers?.ToList() ?? new List<(string, string)> { ("Accept-Encoding", "gzip, deflate") };
			this.streamCompressor = streamCompressor ?? new DefaultStreamCompressor();
		}

		public async Task<Stream> SendRequestAsync(Uri uri, Stream requestStream, CancellationToken cancellationToken = default)
		{
			HttpClient httpClient = await this.GetHttpClientAsync(uri);

			HttpContent httpContent;
			using (StreamReader streamReader = new StreamReader(requestStream))
			{
				string json = await streamReader.ReadToEndAsync();
				httpContent = new StringContent(json, this.Encoding, this.ContentType);
			}
			HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(string.Empty, httpContent, cancellationToken).ConfigureAwait(false);
			httpResponseMessage.EnsureSuccessStatusCode();

			Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync();
			httpResponseMessage.Content.Headers.TryGetValues("Content-Encoding", out var encodings);

			//handle compressions
			if (encodings != null && encodings.Any())
			{
				foreach (string encoding in encodings)
				{
					bool haveType = Enum.TryParse(encoding, true, out CompressionType compressionType);
					if (!haveType)
					{
						continue;
					}

					var decompressedResponseStream = new MemoryStream();
					this.streamCompressor.Decompress(responseStream, decompressedResponseStream, compressionType);
					return decompressedResponseStream;
				}
			}

			// uncompressed, standard response
			return responseStream;
		}

		private async Task<HttpClient> GetHttpClientAsync(Uri uri)
		{
			var httpClient = new HttpClient();
			httpClient.BaseAddress = uri;
			if (this.AuthHeaderValueFactory != null)
			{
				httpClient.DefaultRequestHeaders.Authorization = await this.AuthHeaderValueFactory();
			}

			//attach any extra headers that have been passed
			if (this.Headers != null && this.Headers.Any())
			{
				foreach ((string key, string value) in this.Headers)
				{
					httpClient.DefaultRequestHeaders.Add(key, value);
				}
			}

			return httpClient;
		}



		public static HttpRpcTransportClient CreateUnauthenticated(Encoding encoding = null, string contentType = null, IEnumerable<(string, string)> headers = null)
		{
			return new HttpRpcTransportClient(authHeaderValueFactory: null, encoding: encoding, contentType: contentType, headers: headers);
		}

		public static HttpRpcTransportClient CreateWithBearerAuth(Uri baseUrl, string bearerToken, Encoding encoding = null, string contentType = null, IEnumerable<(string, string)> headers = null)
		{
			var authHeaderValue = AuthenticationHeaderValue.Parse("Bearer " + bearerToken);
			return new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue), encoding: encoding, contentType: contentType, headers: headers);
		}

		public static HttpRpcTransportClient CreateWithBasicAuth(Uri baseUrl, string username, string password, Encoding encoding = null, string contentType = null, IEnumerable<(string, string)> headers = null)
		{
			byte[] headerBytes = Encoding.UTF8.GetBytes(username + ":" + password);
			string a = Convert.ToBase64String(headerBytes);
			var authHeaderValue = AuthenticationHeaderValue.Parse("Basic ");
			return new HttpRpcTransportClient(() => Task.FromResult(authHeaderValue), encoding: encoding, contentType: contentType, headers: headers);
		}
	}

#if !NETSTANDARD1_1
	public class WebSocketRpcTransportClientOptions
	{
		public int MaxBufferSize { get; set; } = 1_000_000;
	}

	public class WebSocketRpcTransportClient : IRpcTransportClient
	{
		private WebSocketRpcTransportClientOptions options { get; }
		public WebSocketRpcTransportClient(IOptions<WebSocketRpcTransportClientOptions> options)
		{
			this.options = options.Value ?? new WebSocketRpcTransportClientOptions();
		}

		public async Task<Stream> SendRequestAsync(Uri uri, Stream requestStream, CancellationToken cancellationToken = default)
		{
			var webSocketClient = new ClientWebSocket();
			await webSocketClient.ConnectAsync(uri, cancellationToken);

			StreamReader streamReader = new StreamReader(requestStream);
			var buffer = new byte[this.options.MaxBufferSize];
			int offset = 0;
			while (true)
			{
				int nextSize = await requestStream.ReadAsync(buffer, offset, this.options.MaxBufferSize, cancellationToken);
				bool endOfMessage = nextSize != this.options.MaxBufferSize;
				await webSocketClient.SendAsync(new ArraySegment<byte>(buffer, 0, nextSize), WebSocketMessageType.Text, endOfMessage, cancellationToken);
				if (endOfMessage)
				{
					break;
				}
				offset += this.options.MaxBufferSize;
			}
			await webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Request finished", cancellationToken);
			return new WebSocketStream(webSocketClient);
		}
	}

	public class WebSocketStream : Stream
	{
		private ClientWebSocket client { get; }
		private bool complete { get; set; }

		public WebSocketStream(ClientWebSocket client)
		{
			this.client = client;
		}

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotImplementedException();

		public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (this.complete)
			{
				return 0;
			}
			WebSocketReceiveResult result = await this.client.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken);
			if (result.MessageType == WebSocketMessageType.Close)
			{
				throw new RpcClientUnknownException("Websocket connection to the server ended prematurely.");
			}
			if (result.EndOfMessage)
			{
				this.complete = true;
			}
			return result.Count;
		}
	}
#endif
}
