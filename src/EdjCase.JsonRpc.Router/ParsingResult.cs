﻿using EdjCase.JsonRpc.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Edjcase.JsonRpc.Router
{
	public class ParsingResult
	{
		/// <summary>
		/// Successfully parsed request
		/// </summary>
		public List<RpcRequest> Requests { get; }
		/// <summary>
		/// Errors with the associated request id
		/// </summary>
		public List<(RpcId Id, RpcError Error)> Errors { get; }
		/// <summary>
		/// Flag to indicate if the request was an array vs singular
		/// </summary>
		public bool IsBulkRequest { get; }
		/// <summary>
		/// Count of total requests processed (successful and failed)
		/// </summary>
		public int RequestCount => this.Requests.Count + this.Errors.Count;

		public ParsingResult(List<RpcRequest> requests, List<(RpcId, RpcError)> errors, bool isBulkRequest)
		{
			this.Requests = requests;
			this.Errors = errors;
			this.IsBulkRequest = isBulkRequest;
		}

		internal static ParsingResult FromResults(List<RpcRequestParseResult> results, bool isBulkRequest)
		{
			var requests = new List<RpcRequest>();
			var errors = new List<(RpcId, RpcError)>();
			foreach (RpcRequestParseResult result in results)
			{
				if (result.Error != null)
				{
					errors.Add((result.Id, result.Error));
				}
				else
				{
					requests.Add(new RpcRequest(result.Id, result.Method, result.Parameters));
				}
			}
			//safety check
			isBulkRequest = isBulkRequest || (requests.Count + errors.Count > 1);
			return new ParsingResult(requests, errors, isBulkRequest);
		}
	}

	internal class RpcRequestParseResult
	{
		public RpcId Id { get; }
		public string Method { get; }
		public RpcParameters Parameters { get; }
		public RpcError Error { get; }
		private RpcRequestParseResult(RpcId id, string method, RpcParameters parameters, RpcError error)
		{
			this.Id = id;
			this.Method = method;
			this.Parameters = parameters;
			this.Error = error;
		}

		public static RpcRequestParseResult Success(RpcId id, string method, RpcParameters parameters)
		{
			return new RpcRequestParseResult(id, method, parameters, null);
		}

		public static RpcRequestParseResult Fail(RpcId id, RpcError error)
		{
			return new RpcRequestParseResult(id, null, default, error);
		}
	}

	public class RpcRequest
	{
		public RpcId Id { get; }
		public string Method { get; }
		public RpcParameters Parameters { get; }
		public RpcRequest(RpcId id, string method, RpcParameters parameters = null)
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

		public RpcParameters(List<IRpcParameter> parameters)
		{
			this.Value = parameters ?? throw new ArgumentNullException(nameof(parameters));
			this.IsDictionary = false;
		}

		public RpcParameters(params IRpcParameter[] parameters)
		{
			this.Value = parameters?.ToList() ?? throw new ArgumentNullException(nameof(parameters));
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

		public List<IRpcParameter> AsList
		{
			get
			{
				this.CheckValue(isDictionary: false);
				return (List<IRpcParameter>)this.Value;
			}
		}

		private void CheckValue(bool isDictionary)
		{
			if (isDictionary != this.IsDictionary)
			{
				throw new InvalidOperationException();
			}
		}
	}

	public interface IRpcParameter
	{
		bool TryGetValue(Type type, out object value);
	}

	public static class RpcParameterExtensions
	{
		public static bool TryGetValue<T>(this IRpcParameter parameter, out T value)
		{
			bool parsed = parameter.TryGetValue(typeof(T), out object v);
			if (parsed)
			{
				value = (T)v;
				return true;
			}
			value = default;
			return false;
		}
	}

	public class RawRpcParameter : IRpcParameter
	{
		public object Value { get; }
		public RawRpcParameter(object value)
		{
			this.Value = value;
		}
		public bool TryGetValue(Type type, out object value)
		{
			if (this.Value == null)
			{
				value = null;
				return true;
			}
			if (this.Value.GetType() == type)
			{
				value = this.Value;
				return true;
			}
			TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
			if (typeConverter.CanConvertTo(type))
			{
				value = typeConverter.ConvertTo(this.Value, type);
				return true;
			}
			value = default;
			return false;
		}
	}

	public class SerializedRpcParameter : IRpcParameter
	{
		public ReadOnlyMemory<byte> Value { get; }
		public JsonSerializerOptions Options { get; }

		public SerializedRpcParameter(ReadOnlyMemory<byte> value, JsonSerializerOptions options)
		{
			this.Value = value;
			this.Options = options;
		}

		public bool TryGetValue(Type type, out object value)
		{
			try
			{
				value = System.Text.Json.JsonSerializer.Deserialize(this.Value.Span, type, this.Options);
			}
			catch (Exception)
			{
				value = default;
				return false;
			}
			return true;
		}
	}
}
