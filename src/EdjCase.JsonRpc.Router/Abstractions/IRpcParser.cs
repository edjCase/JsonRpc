using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Utilities;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	internal interface IRpcParser
	{
		/// <summary>
		/// Parses all the requests from the json in the request
		/// </summary>
		/// <param name="jsonStream">Json stream to parse from</param>
		/// <returns>Result of the parsing. Includes all the parsed requests and any errors</returns>
		ParsingResult ParseRequests(Stream jsonStream);

		public virtual ParsingResult ParseRequests(string json)
		{
			using (var stream = StreamUtil.GetStreamFromUtf8String(json ?? string.Empty))
			{
				return this.ParseRequests(stream);
			}
		}
	}
}
