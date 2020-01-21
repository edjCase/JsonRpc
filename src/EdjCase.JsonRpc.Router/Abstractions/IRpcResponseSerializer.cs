using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcResponseSerializer
	{
		Task SerializeAsync(RpcResponse response, Stream stream);
		Task SerializeBulkAsync(IEnumerable<RpcResponse> responses, Stream stream);

		public virtual async Task<string> SerializeAsync(RpcResponse response)
		{
			using var stream = new MemoryStream();
			await this.SerializeAsync(response, stream);
			return await IRpcResponseSerializer.GetStringAsync(stream);
		}

		public virtual async Task<string> SerializeBulkAsync(IEnumerable<RpcResponse> responses)
		{
			using var stream = new MemoryStream();
			await this.SerializeBulkAsync(responses, stream);
			return await IRpcResponseSerializer.GetStringAsync(stream);
		}

		private static Task<string> GetStringAsync(MemoryStream stream)
		{
			stream.Position = 0;
			using var streamReader = new StreamReader(stream);
			return streamReader.ReadToEndAsync();
		}
	}
}
