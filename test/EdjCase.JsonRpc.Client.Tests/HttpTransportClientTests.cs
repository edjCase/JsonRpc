using System;
using Xunit;
using EdjCase.JsonRpc.Client;
using Moq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Options;
using EdjCase.JsonRpc.Common.Tools;

namespace EdjCase.JsonRpc.Client.Tests
{
	public class HttpTransportClientTests
	{
		[Fact]
		public async Task InvalidExceptionWithBadStatusCode()
		{
			var uri = new Uri("https://test.com/test");
			foreach (HttpStatusCode statusCode in Enum.GetValues(typeof(HttpStatusCode)).Cast<HttpStatusCode>())
			{
				var responseMessage = new HttpResponseMessage(statusCode);
				responseMessage.Content = new StringContent("Bad");
				var fakeHandler = new FakeResponseHandler();
				fakeHandler.AddFakeResponse(uri, responseMessage);
				var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
				factory
				.Setup(f => f.CreateClient(Options.DefaultName))
				.Returns(new HttpClient(fakeHandler));
				var streamCompressor = new Mock<IStreamCompressor>(MockBehavior.Strict);
				System.IO.Stream stream;
				streamCompressor
					.Setup(c => c.TryGetCompressionStream(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<System.IO.Compression.CompressionMode>(), out stream))
					.Returns(false);
				var client = new HttpRpcTransportClient(streamCompressor.Object, httpClientFactory: factory.Object);
				Func<Task> func = () => client.SendRequestAsync(uri, "{}");
				if (!responseMessage.IsSuccessStatusCode)
				{
					await Assert.ThrowsAsync<RpcClientInvalidStatusCodeException>(func);
				}
				else
				{
					await func();
				}
			}
		}
	}

	public class FakeResponseHandler : DelegatingHandler
	{
		private readonly Dictionary<Uri, HttpResponseMessage> responses = new Dictionary<Uri, HttpResponseMessage>();

		public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage)
		{
			responses.Add(uri, responseMessage);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			HttpResponseMessage message;
			if (responses.ContainsKey(request.RequestUri))
			{
				message = responses[request.RequestUri];
			}
			else
			{
				message = new HttpResponseMessage(HttpStatusCode.NotFound)
				{
					RequestMessage = request
				};
			}
			return Task.FromResult(message);
		}
	}
}
