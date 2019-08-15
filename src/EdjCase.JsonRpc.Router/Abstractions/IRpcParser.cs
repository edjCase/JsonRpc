using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Edjcase.JsonRpc.Router;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcParser
	{
		/// <summary>
		/// Parses all the requests from the json in the request
		/// </summary>
		/// <param name="jsonStream">Json stream to parse from</param>
		/// <returns>Result of the parsing. Includes all the parsed requests and any errors</returns>
		ParsingResult ParseRequests(Stream jsonStream);
	}

	public static class RpcParserExtensions
	{
		public static ParsingResult ParseRequests(this IRpcParser parser, string json)
		{
			byte[] bytes = json == null ? new byte[0] : Encoding.UTF8.GetBytes(json);
			using (var stream = new MemoryStream(bytes))
			{
				return parser.ParseRequests(stream);
			}
		}
	}
}
