namespace edjCase.JsonRpc.Router
{
	public enum RpcErrorCode
	{
		ParseError = -32700,
		InvalidRequest = -32600,
		MethodNotFound = -32601,
		InvalidParams = -32602,
		InternalError = -32603,

		//Custom
		AmbiguousMethod = -32000
	}
}
