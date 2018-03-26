using System;
using System.Collections.Generic;
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
		/// <param name="jsonString">Json from the http request</param>
		/// <returns>Result of the parsing. Includes all the parsed requests and any errors</returns>
		ParsingResult ParseRequests(string jsonString);
	}
}
