using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EdjCase.JsonRpc.Core;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcResponseSerializer
	{
		void Serialize(RpcResponse response, Stream stream);
		void SerializeBulk(IEnumerable<RpcResponse> responses, Stream stream);
	}

	public static class RpcResponseSerializerExtensions
	{
		public static string Serialize(this IRpcResponseSerializer serializer, RpcResponse response)
		{
			using (var stream = new MemoryStream())
			{
				serializer.Serialize(response, stream);
				return RpcResponseSerializerExtensions.GetString(stream);
			}
		}

		public static string SerializeBulk(this IRpcResponseSerializer serializer, IEnumerable<RpcResponse> responses)
		{
			using (var stream = new MemoryStream())
			{
				serializer.SerializeBulk(responses, stream);
				return RpcResponseSerializerExtensions.GetString(stream);
			}
		}

		private static string GetString(MemoryStream stream)
		{
			stream.Position = 0;
			return new StreamReader(stream).ReadToEnd();
		}
	}
}
