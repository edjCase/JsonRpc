namespace EdjCase.JsonRpc.Core
{
	/// <summary>
	/// Error codes for different Rpc errors
	/// </summary>
	public enum RpcErrorCode
	{
		ParseError = -32700,
		InvalidRequest = -32600,
		MethodNotFound = -32601,
		InvalidParams = -32602,
		InternalError = -32603
	}

	public static class JsonRpcContants
	{		
		public const string VersionPropertyName = "jsonrpc";
		public const string MethodPropertyName = "method";
		public const string ParamsPropertyName = "params";
		public const string IdPropertyName = "id";
		public const string ResultPropertyName = "result";
		public const string ErrorPropertyName = "error";
		public const string ErrorCodePropertyName = "code";
		public const string ErrorMessagePropertyName = "message";
		public const string ErrorDataPropertyName = "data";
	}


	public enum CompressionType
	{
		Gzip,
		Deflate
	}
}
