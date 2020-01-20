using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Router.Utilities;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcRequestHandler
	{
		Task<bool> HandleRequestAsync(Stream requestBody, Stream responseBody);

		public virtual async Task<string?> HandleRequestAsync(string requestJson)
		{
			using (var requestStream = StreamUtil.GetStreamFromUtf8String(requestJson))
			{
				using (var responseStream = new MemoryStream())
				{
					bool hasResponse = await this.HandleRequestAsync(requestStream, responseStream);
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
