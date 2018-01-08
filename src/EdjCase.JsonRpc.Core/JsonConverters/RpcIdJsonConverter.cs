using System;
using Newtonsoft.Json;
using EdjCase.JsonRpc.Core.Utilities;

namespace EdjCase.JsonRpc.Core.JsonConverters
{
	/// <summary>
	/// Converter to convert and enforce the id to be a string, number or null
	/// </summary>
	public class RpcIdJsonConverter : JsonConverter
	{
		/// <summary>
		/// Writes the value of the id to json format
		/// </summary>
		/// <param name="writer">Json writer</param>
		/// <param name="value">Value to be converted to json format</param>
		/// <param name="serializer">Json serializer</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			RpcId id = this.ValidateValue(value);
			writer.WriteValue(id.Value);
		}

		/// <summary>
		/// Read the json format and return the correct object type/value for it
		/// </summary>
		/// <param name="reader">Json reader</param>
		/// <param name="objectType">Type of property being set</param>
		/// <param name="existingValue">The current value of the property being set</param>
		/// <param name="serializer">Json serializer</param>
		/// <returns>The object value of the converted json value</returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return this.ValidateValue(reader.Value);
		}

		/// <summary>
		/// Validates that the value is a string, number or null and converts emtpy strings to null
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="RpcInvalidRequestException">Thrown when value is not a string, number or null</exception>
		/// <returns>The same value or null if it is a string and empty</returns>
		private RpcId ValidateValue(object value)
		{
			if (value == null)
			{
				return default;
			}
			if(value is RpcId rpcId)
			{
				return rpcId;
			}
			if(value is string stringValue)
			{
				return new RpcId(stringValue);
			}
			if (value.GetType().IsNumericType())
			{
				return new RpcId(Convert.ToDouble(value));
			}
			throw new RpcInvalidRequestException("Id must be a string, a number or null.");
		}

		/// <summary>
		/// Determines if the type can be convertered with this converter
		/// </summary>
		/// <param name="objectType">Type of the object</param>
		/// <returns>True if the converter converts the specified type, otherwise False</returns>
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof (string) || objectType.IsNumericType();
		}

	}
}
