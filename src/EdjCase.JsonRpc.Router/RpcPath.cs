using EdjCase.JsonRpc.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EdjCase.JsonRpc.Router
{
	/// <summary>
	/// Represents the url path for Rpc routing purposes
	/// </summary>
	public class RpcPath : IEquatable<RpcPath>
	{
		private char[] path;

		private int? hashCodeCache;

		private RpcPath(char[] path)
		{
			if (path == null || path.Length < 1)
			{
				throw new ArgumentNullException(nameof(path));
			}
			this.path = path;
		}

		public static bool operator ==(RpcPath? path1, RpcPath? path2)
		{
			if (object.ReferenceEquals(path1, null))
			{
				return object.ReferenceEquals(path2, null);
			}
			return path1.Equals(path2);
		}

		public static bool operator !=(RpcPath? path1, RpcPath? path2)
		{
			return !(path1 == path2);
		}

		public bool StartsWith(RpcPath? other)
		{
			if (other == null)
			{
				return true;
			}
			if (other.path.Length > this.path.Length)
			{
				return false;
			}
			for (int i = 0; i < other.path.Length; i++)
			{
				if (other.path[i] != this.path[i])
				{
					return false;
				}
			}
			return true;
		}

		public bool Equals(RpcPath? other)
		{
			if (object.ReferenceEquals(other, null))
			{
				return false;
			}
			return this.GetHashCode() == other.GetHashCode();
		}


		public override bool Equals(object? obj)
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
				int hash = 1337;
				foreach (char component in this.path)
				{
					hash = (hash * 7) + component.GetHashCode();
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
		public static RpcPath Parse(ReadOnlySpan<char> path)
		{
			if (!RpcPath.TryParse(path, out RpcPath? rpcPath))
			{
				throw new RpcException(RpcErrorCode.ParseError, $"Rpc path could not be parsed from '{new string(path.ToArray())}'.");
			}
			return rpcPath!;
		}
		/// <summary>
		/// Creates a <see cref="RpcPath"/> based on the string form of the path
		/// </summary>
		/// <param name="path">Uri/route path</param>
		/// <returns>True if the path parses, otherwise false</returns>
		public static bool TryParse(ReadOnlySpan<char> path, out RpcPath? rpcPath)
		{
			if (path.IsEmpty)
			{
				rpcPath = null;
				return true;
			}
			else
			{
				try
				{
					int start = IsSlash(path[0]) ? 1 : 0;
					if (path.Length <= start)
					{
						rpcPath = default;
						return true;
					}
					int length = IsSlash(path[path.Length - 1]) ? path.Length - 1 : path.Length;
					if (start >= length - 1)
					{
						rpcPath = default;
						return true;
					}


					var charArray = new char[length - start];

					int j = 0;
					for (int i = start; i < length; i++)
					{
						char c;
						if (char.IsWhiteSpace(path[i]))
						{
							rpcPath = default;
							return false;
						}
						else if (IsSlash(path[i]))
						{
							//make all the slashes the same
							c = '/';
						}
						else
						{
							c = char.ToLowerInvariant(path[i]);
						}

						charArray[j] = c;
						j++;
					}
					rpcPath = new RpcPath(charArray);
					return true;
				}
				catch
				{
					rpcPath = default;
					return false;
				}
			}
			bool IsSlash(char c)
			{
				return c == '/' || c == '\\';
			}
		}

		/// <summary>
		/// Removes the base path path from this path
		/// </summary>
		/// <param name="basePath">Base path to remove</param>
		/// <returns>A new path that is the full path without the base path</returns>
		public RpcPath? RemoveBasePath(RpcPath basePath)
		{
			if (!this.TryRemoveBasePath(basePath, out RpcPath? path))
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
		public bool TryRemoveBasePath(RpcPath? basePath, out RpcPath? path)
		{
			if (basePath == null)
			{
				path = this.Clone();
				return true;
			}
			if (!this.StartsWith(basePath))
			{
				path = default;
				return false;
			}
			int size = this.path.Length - basePath.path.Length;
			if (size < 1)
			{
				path = default;
				return true;
			}
			//Removes the / as well
			var newComponents = new char[size - 1];
			this.path.AsSpan(basePath.path.Length + 1).CopyTo(newComponents);
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
			if (other == null)
			{
				return this.Clone();
			}
			char[] newComponents = new char[this.path.Length + other.path.Length + 1];
			this.path.CopyTo(newComponents, 0);
			newComponents[this.path.Length] = '/';
			other.path.CopyTo(newComponents, this.path.Length + 1);
			return new RpcPath(newComponents);
		}

		public override string ToString()
		{
			return new string(this.path);
		}

		public RpcPath Clone()
		{
			var newComponents = new char[this.path.Length];
			this.path.CopyTo(newComponents, 0);
			return new RpcPath(newComponents);
		}

		public static implicit operator string(RpcPath path)
		{
			return path.ToString();
		}

		public static implicit operator RpcPath(string s)
		{
			return RpcPath.Parse(s.AsSpan());
		}
	}
}
