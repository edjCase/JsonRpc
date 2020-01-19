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
		public RpcParameters? Parameters { get; }
		public RpcRequest(RpcId id, string method, RpcParameters? parameters = null)
		{
			this.Id = id;
			this.Method = method;
			this.Parameters = parameters;
		}
	}

	public class RpcParameters
	{
		public object Value { get; }
		public bool IsDictionary { get; }

		public RpcParameters(Dictionary<string, IRpcParameter> parameters)
		{
			this.Value = parameters ?? throw new ArgumentNullException(nameof(parameters));
			this.IsDictionary = true;
		}

		public RpcParameters(IRpcParameter parameter)
		{
			if (parameter == null)
			{
				throw new ArgumentNullException(nameof(parameter));
			}
			this.Value = new IRpcParameter[1] { parameter };
			this.IsDictionary = false;
		}

		public RpcParameters(params IRpcParameter[] parameters)
		{
			this.Value = parameters ?? throw new ArgumentNullException(nameof(parameters));
			this.IsDictionary = false;
		}

		public Dictionary<string, IRpcParameter> AsDictionary
		{
			get
			{
				this.CheckValue(isDictionary: true);
				return (Dictionary<string, IRpcParameter>)this.Value;
			}
		}

		public IRpcParameter[] AsArray
		{
			get
			{
				this.CheckValue(isDictionary: false);
				return (IRpcParameter[])this.Value;
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
