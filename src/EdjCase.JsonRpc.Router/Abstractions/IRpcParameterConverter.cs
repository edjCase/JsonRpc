using System;
using System.Collections.Generic;
using System.Text;

namespace EdjCase.JsonRpc.Router.Abstractions
{
	public interface IRpcParameterConverter
	{
		bool AreTypesCompatible(RpcParameterType sourceType, RpcParameterType destinationType);
		bool TryConvertValue(RpcParameter sourceValue, RpcParameterType destinationType, Type destinationRawType, out object? destinationValue,  out Exception? exception);
		RpcParameterType GetRpcParameterType(Type type);
	}
}
