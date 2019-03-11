using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using EdjCase.JsonRpc.Core.Tools;

namespace EdjCase.JsonRpc.Client
{
	public class RpcEvents
	{
		public Func<RequestEventContext, Task> OnRequestStartAsync { get; set; }
		public Func<ResponseEventContext, RequestEventContext, Task> OnRequestCompleteAsync { get; set; }
	}

	public class RequestEventContext
	{
		public List<RpcRequest> Requests { get; }
	}

	public class ResponseEventContext
	{
		public TimeSpan Duration { get; }
		public List<RpcResponse> Responses { get; }
		public Exception ClientError { get; }

		public ResponseEventContext(TimeSpan duration, List<RpcResponse> responses, Exception clientError = null)
		{
			this.Duration = duration;
			this.Responses = responses;
			this.ClientError = clientError;
		}
	}
}