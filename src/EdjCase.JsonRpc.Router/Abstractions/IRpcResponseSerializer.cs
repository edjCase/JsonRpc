using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Common;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcResponseSerializer
	{
		Task SerializeAsync(RpcResponse response, Stream stream);
		Task SerializeBulkAsync(IEnumerable<RpcResponse> responses, Stream stream);
	}

	public static class RpcResponseSerializerExtensions
	{
		public static async Task<string> SerializeAsync(this IRpcResponseSerializer serializer, RpcResponse response)
		{
			using (var stream = new MemoryStream())
			{
				await serializer.SerializeAsync(response, stream);
				return await RpcResponseSerializerExtensions.GetStringAsync(stream);
			}
		}

		public static async Task<string> SerializeBulkAsync(this IRpcResponseSerializer serializer, IEnumerable<RpcResponse> responses)
		{
			using (var stream = new MemoryStream())
			{
				await serializer.SerializeBulkAsync(responses, stream);
				return await RpcResponseSerializerExtensions.GetStringAsync(stream);
			}
		}

		private static Task<string> GetStringAsync(MemoryStream stream)
		{
			stream.Position = 0;
			return new StreamReader(stream).ReadToEndAsync();
		}
	}
}
