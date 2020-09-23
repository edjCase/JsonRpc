using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Swagger.Extensions
{
    public static class Extensions
    {
        public static Type GetReturnMethodType(this MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType.IsGenericType  && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return methodInfo.ReturnType.GenericTypeArguments.First();
            }
            else if (methodInfo.ReturnType == typeof(Task))
            {
                return typeof(void);
            }
            else
            {
                return methodInfo.ReturnType;
            }
        }
    }
}