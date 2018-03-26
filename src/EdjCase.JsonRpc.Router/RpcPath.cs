using EdjCase.JsonRpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Represents the url path for Rpc routing purposes
	/// </summary>
	public struct RpcPath : IEquatable<RpcPath>
	{
		/// <summary>
		/// Default/Empty path
		/// </summary>
		public static RpcPath Default => new RpcPath();

		/// <summary>
		/// Path components split on forward slashes
		/// </summary>
		private readonly string[] componentsValue;

		private int? hashCodeCache;

		/// <param name="components">Uri components for the path</param>
		private RpcPath(string[] components = null)
		{
			this.componentsValue = components ?? new string[0];
			this.hashCodeCache = null;
		}

		public static bool operator ==(RpcPath path1, RpcPath path2)
		{
			return path1.Equals(path2);
		}

		public static bool operator !=(RpcPath path1, RpcPath path2)
		{
			return !path1.Equals(path2);
		}

		public bool StartsWith(RpcPath other)
		{
			if ((other.componentsValue?.Length ?? 0) == 0)
			{
				return true;
			}
			if ((this.componentsValue?.Length ?? 0) == 0)
			{
				return false;
			}
			if (other.componentsValue.Length > this.componentsValue.Length)
			{
				return false;
			}
			for (int i = 0; i < other.componentsValue.Length; i++)
			{
				string component = this.componentsValue[i];
				string otherComponent = other.componentsValue[i];
				if (!string.Equals(component, otherComponent))
				{
					return false;
				}
			}
			return true;
		}

		public bool Equals(RpcPath other)
		{
			return this.GetHashCode() == other.GetHashCode();
		}
		

		public override bool Equals(object obj)
		{
			if (obj is RpcPath path)
			{
				return this.Equals(path);
			}
			return false;
		}


		public override int GetHashCode()
		{
			//TODO best way to optimize gethashcode? multithread?
			if (this.hashCodeCache == null)
			{
				int hash;
				if (this.componentsValue == null || this.componentsValue.Length == 0)
				{
					hash = 0;
				}
				else
				{
					hash = 1337;
					foreach (string component in this.componentsValue)
					{
						hash = (hash * 7) + component.GetHashCode();
					}
				}
				this.hashCodeCache = hash;
			}
			return this.hashCodeCache.Value;
		}

		/// <summary>
		/// Creates a <see cref="RpcPath"/> based on the string form of the path
		/// </summary>
		/// <param name="path">Uri/route path</param>
		/// <returns>Rpc path based on the path string</returns>
		public static RpcPath Parse(string path)
		{
			if (!RpcPath.TryParse(path, out RpcPath rpcPath))
			{
				throw new RpcException(RpcErrorCode.ParseError, $"Rpc path could not be parsed from '{path}'.");
			}
			return rpcPath;
		}
		/// <summary>
		/// Creates a <see cref="RpcPath"/> based on the string form of the path
		/// </summary>
		/// <param name="path">Uri/route path</param>
		/// <returns>True if the path parses, otherwise false</returns>
		public static bool TryParse(string path, out RpcPath rpcPath)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				rpcPath = new RpcPath();
				return true;
			}
			else
			{
				try
				{
					string[] pathComponents = path
						.ToLowerInvariant()
						.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
					rpcPath = new RpcPath(pathComponents);
					return true;
				}
				catch
				{
					rpcPath = default;
					return false;
				}
			}
		}

		/// <summary>
		/// Removes the base path path from this path
		/// </summary>
		/// <param name="basePath">Base path to remove</param>
		/// <returns>A new path that is the full path without the base path</returns>
		public RpcPath RemoveBasePath(RpcPath basePath)
		{
			if (!this.TryRemoveBasePath(basePath, out RpcPath path))
			{
				throw new RpcException(RpcErrorCode.ParseError, $"Count not remove path '{basePath}' from path '{this}'.");
			}
			return path;
		}

		/// <summary>
		/// Tries to remove the base path path from this path
		/// </summary>
		/// <param name="basePath">Base path to remove</param>
		/// <returns>True if removed the base path. Otherwise false</returns>
		public bool TryRemoveBasePath(RpcPath basePath, out RpcPath path)
		{
			if (basePath == default)
			{
				path = this.Clone();
				return true;
			}
			if (!this.StartsWith(basePath))
			{
				path = default;
				return false;
			}
			var newComponents = new string[this.componentsValue.Length - basePath.componentsValue.Length];
			if (newComponents.Length > 0)
			{
				Array.Copy(this.componentsValue, basePath.componentsValue.Length, newComponents, 0, newComponents.Length);
			}
			path = new RpcPath(newComponents);
			return true;
		}

		/// <summary>
		/// Merges the two paths to create a new Rpc path that is the combination of the two
		/// </summary>
		/// <param name="other">Other path to add to the end of the current path</param>
		/// <returns>A new path that is the combination of the two paths</returns>
		public RpcPath Add(RpcPath other)
		{
			if (other.componentsValue == null)
			{
				return this.Clone();
			}
			if (this.componentsValue == null)
			{
				return other.Clone();
			}
			int componentCount = this.componentsValue.Length + other.componentsValue.Length;
			string[] newComponents = new string[componentCount];
			this.componentsValue.CopyTo(newComponents, 0);
			other.componentsValue.CopyTo(newComponents, this.componentsValue.Length);
			return new RpcPath(newComponents);
		}

		public override string ToString()
		{
			if (this.componentsValue == null)
			{
				return "/";
			}
			return "/" + string.Join("/", this.componentsValue);
		}

		public RpcPath Clone()
		{
			if (this.componentsValue == null || this.componentsValue.Length == 0)
			{
				return new RpcPath();
			}
			int componentCount = this.componentsValue.Length;
			string[] newComponents = new string[componentCount];
			this.componentsValue.CopyTo(newComponents, 0);
			return new RpcPath(newComponents);
		}

		public static implicit operator string(RpcPath path)
		{
			return path.ToString();
		}

		public static implicit operator RpcPath(string s)
		{
			return RpcPath.Parse(s);
		}
	}
}
