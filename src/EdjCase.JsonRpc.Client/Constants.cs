
using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Client
{
	public static class Defaults
	{
		public const string ContentType = "application/json";
		public static readonly Encoding Encoding = Encoding.UTF8;
		public static List<(string, string)> GetHeaders() => new List<(string, string)> { ("Accept-Encoding", "gzip, deflate") };
	}
}