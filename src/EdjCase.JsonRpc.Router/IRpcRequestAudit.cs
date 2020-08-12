using System;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router
{
    public interface IRpcRequestAuditHandler
    {
        Task HandleAsync(RpcPath? path, RpcRequest request, RpcResponse? response, TimeSpan executionTime);
    }
}
