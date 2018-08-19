using System;
using EdjCase.JsonRpc.Core;

namespace EdjCase.JsonRpc.Client
{
    /// <summary>
    /// Base exception that is thrown from an error that was caused by the client
    /// for the rpc request (not caused by rpc server)
    /// </summary>
    public abstract class RpcClientException : Exception
    {
        /// <param name="message">Error message</param>
        protected RpcClientException(string message) : base(message)
        {

        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        protected RpcClientException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }

    /// <summary>
    /// Exception for all unknown exceptions that were thrown by the client
    /// </summary>
    public class RpcClientUnknownException : RpcClientException
    {
        /// <param name="message">Error message</param>
        public RpcClientUnknownException(string message) : base(message)
        {
        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public RpcClientUnknownException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for all parsing exceptions that were thrown by the client
    /// </summary>
    public class RpcClientParseException : RpcClientException
    {
        /// <param name="message">Error message</param>
        public RpcClientParseException(string message) : base(message)
        {
        }

        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public RpcClientParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for all bad http status codes that were thrown by the client
    /// </summary>
    public class RpcClientInvalidStatusCodeException : RpcClientException
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public string Content { get; }

        /// <param name="statusCode">Http Status Code</param>
        /// <param name="innerException">Inner exception</param>
        public RpcClientInvalidStatusCodeException(System.Net.HttpStatusCode statusCode, string content)
        : base($"The server returned an invalid status code of '{statusCode}'. Response content: {content}.")
        {
            StatusCode = statusCode;
            Content = content;
        }
    }
}
