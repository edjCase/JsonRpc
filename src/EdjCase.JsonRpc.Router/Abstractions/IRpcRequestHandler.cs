using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Utilities;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcRequestHandler
	{
		/// <summary>
		/// Takes in the request bytes and context, invokes the rpc request and then 
		/// sets the response bytes if there is a response
		/// </summary>
		/// <param name="context">Contextual information about the request being handled</param>
		/// <param name="requestBody">The request byte stream</param>
		/// <param name="responseBody">An writable stream to write the response to</param>
		/// <returns>True if there is a response. If false, no bytes will be written to the stream</returns>
		Task<bool> HandleRequestAsync(RpcContext context, Stream requestBody, Stream responseBody);

		/// <summary>
		/// Takes in the request bytes and context, invokes the rpc request and then returns
		/// the response bytes if there is a response
		/// </summary>
		/// <param name="context">Contextual information about the request being handled</param>
		/// <param name="requestBody">The request bytes</param>
		/// <returns>The response bytes or null (if there is no response)</returns>
		public virtual async Task<byte[]?> HandleRequestAsync(RpcContext context, byte[] requestBody)
		{
			using (var requestStream = new MemoryStream(requestBody))
			{
				using (var responseStream = new MemoryStream())
				{
					bool hasResponse = await this.HandleRequestAsync(context, requestStream, responseStream);
					if (!hasResponse)
					{
						return null;
					}
					responseStream.Position = 0;
					return responseStream.ToArray();
				}
			}
		}

		/// <summary>
		/// Takes in the request json and context, invokes the rpc request and then returns
		/// the response json if there is a response
		/// </summary>
		/// <param name="context">Contextual information about the request being handled</param>
		/// <param name="requestJson">The request json</param>
		/// <returns>The response json or null (if there is no response)</returns>
		public virtual async Task<string?> HandleRequestAsync(RpcContext context, string requestJson)
		{
			using (var requestStream = StreamUtil.GetStreamFromUtf8String(requestJson))
			{
				using (var responseStream = new MemoryStream())
				{
					bool hasResponse = await this.HandleRequestAsync(context, requestStream, responseStream);
					if (!hasResponse)
					{
						return null;
					}
					responseStream.Position = 0;
					using (var stream = new StreamReader(responseStream))
					{
						return await stream.ReadToEndAsync();
					}
				}
			}
		}
	}
}
