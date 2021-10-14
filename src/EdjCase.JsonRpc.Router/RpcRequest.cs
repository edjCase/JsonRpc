using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EdjCase.JsonRpc.Router
{
	public class RpcRequest
	{
		public RpcId Id { get; }
		public string Method { get; }
		public TopLevelRpcParameters? Parameters { get; }
		public RpcRequest(RpcId id, string method, TopLevelRpcParameters? parameters = null)
		{
			this.Id = id;
			this.Method = method;
			this.Parameters = parameters;
		}
	}

	public class TopLevelRpcParameters
	{
		public object Value { get; }
		public bool IsDictionary { get; }

		public TopLevelRpcParameters(Dictionary<string, RpcParameter> parameters)
		{
			this.Value = parameters ?? throw new ArgumentNullException(nameof(parameters));
			this.IsDictionary = true;
		}

		public TopLevelRpcParameters(params RpcParameter[] parameters)
		{
			this.Value = parameters ?? throw new ArgumentNullException(nameof(parameters));
			this.IsDictionary = false;
		}

		public Dictionary<string, RpcParameter> AsDictionary
		{
			get
			{
				this.CheckValue(isDictionary: true);
				return (Dictionary<string, RpcParameter>)this.Value;
			}
		}

		public RpcParameter[] AsArray
		{
			get
			{
				this.CheckValue(isDictionary: false);
				return (RpcParameter[])this.Value;
			}
		}

		private void CheckValue(bool isDictionary)
		{
			if (isDictionary != this.IsDictionary)
			{
				throw new InvalidOperationException();
			}
		}

		public bool Any()
		{
			if (this.IsDictionary)
			{
				return this.AsDictionary.Any();
			}
			return this.AsArray.Any();
		}
	}
}
