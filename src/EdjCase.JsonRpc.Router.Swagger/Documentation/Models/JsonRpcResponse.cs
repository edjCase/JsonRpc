namespace EdjCase.JsonRpc.Router.Swagger.Documentation.Models
{
    public class JsonRpcResponse<T>
    {
        public string id { get; set; }
        public string jsonrpc { get; set; }
        public T result { get; set; }
    }
}