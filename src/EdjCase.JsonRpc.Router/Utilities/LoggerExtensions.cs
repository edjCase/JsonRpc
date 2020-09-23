using EdjCase.JsonRpc.Common;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EdjCase.JsonRpc.Router.Utilities
{
	internal static class LoggerExtensions
	{
		private static readonly Action<ILogger, string, Exception?> attemptingToMatchMethod;
		private static readonly Action<ILogger, Exception?> requestMatchedMethod;
		private static readonly Action<ILogger, string, Exception?> methodsInRoute;
		private const LogLevel methodsInRouteLevel = LogLevel.Trace;
		private static readonly Action<ILogger, int, Exception?> invokingBatchRequests;
		private static readonly Action<ILogger, Exception?> batchRequestsComplete;
		private static readonly Action<ILogger, RpcId, Exception?> invokingRequest;
		private static readonly Action<ILogger, string, Exception?> invokeMethod;
		private static readonly Action<ILogger, string, Exception?> invokeMethodComplete;
		private static readonly Action<ILogger, RpcId, Exception?> finishedRequest;
		private static readonly Action<ILogger, Exception?> finishedRequestNoId;
		private static readonly Action<ILogger, Exception?> skippingAuth;
		private static readonly Action<ILogger, Exception?> runningAuth;
		private static readonly Action<ILogger, Exception?> authSuccessful;
		private static readonly Action<ILogger, Exception?> authFailed;
		private static readonly Action<ILogger, Exception?> noConfiguredAuth;
		private static readonly Action<ILogger, Exception?> parsingRequests;
		private static readonly Action<ILogger, int, Exception?> parsedRequests;
		private static readonly Action<ILogger, int, Exception?> processingRequests;
		private static readonly Action<ILogger, int, string, Exception?> responseFailedWithNoId;
		private static readonly Action<ILogger, Exception?> noResponses;
		private static readonly Action<ILogger, int, Exception?> responses;

		static LoggerExtensions()
		{
			attemptingToMatchMethod = LoggerMessage.Define<string>(
				LogLevel.Debug,
				new EventId(1, nameof(AttemptingToMatchMethod)),
				"Attempting to match Rpc request to a method '{Method}'");

			requestMatchedMethod = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(2, nameof(RequestMatchedMethod)),
				"Request was matched to a method");

			methodsInRoute = LoggerMessage.Define<string>(
				LoggerExtensions.methodsInRouteLevel,
				new EventId(3, nameof(MethodsInRoute)),
				"Methods in route: {MethodsString}");

			invokingBatchRequests = LoggerMessage.Define<int>(
				LogLevel.Debug,
				new EventId(4, nameof(InvokingBatchRequests)),
				"Invoking '{Count}' batch requests");

			batchRequestsComplete = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(5, nameof(BatchRequestsComplete)),
				"Finished batch requests");

			invokingRequest = LoggerMessage.Define<RpcId>(
				LogLevel.Debug,
				new EventId(6, nameof(InvokingRequest)),
				"Invoking request with id '{Id}'");

			invokeMethod = LoggerMessage.Define<string>(
				LogLevel.Debug,
				new EventId(7, nameof(InvokeMethod)),
				"Attempting to invoke method '{Method}'");

			invokeMethodComplete = LoggerMessage.Define<string>(
				LogLevel.Debug,
				new EventId(8, nameof(InvokeMethodComplete)),
				"Finished invoking method '{Method}'");

			finishedRequest = LoggerMessage.Define<RpcId>(
				LogLevel.Debug,
				new EventId(9, nameof(FinishedRequest)),
				"Finished request with id: {Id}");

			finishedRequestNoId = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(10, nameof(FinishedRequestNoId)),
				"Finished request with no id. Not returning a response");

			skippingAuth = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(11, nameof(SkippingAuth)),
				"Skipping authorization. Allow anonymous specified for method.");

			runningAuth = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(12, nameof(RunningAuth)),
				"Running authorization for method.");

			authSuccessful = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(13, nameof(AuthSuccessful)),
				"Authorization was successful.");

			authFailed = LoggerMessage.Define(
				LogLevel.Information,
				new EventId(14, nameof(AuthFailed)),
				"Authorization failed.");

			noConfiguredAuth = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(15, nameof(NoConfiguredAuth)),
				"Skipping authorization. None configured for class or method.");

			parsingRequests = LoggerMessage.Define(
				LogLevel.Debug,
				new EventId(16, nameof(ParsingRequests)),
				"Attempting to parse Rpc request from the json");

			parsedRequests = LoggerMessage.Define<int>(
				LogLevel.Debug,
				new EventId(17, nameof(ParsedRequests)),
				"Successfully parsed {Count} Rpc request(s)");

			processingRequests = LoggerMessage.Define<int>(
				LogLevel.Information,
				new EventId(18, nameof(ProcessingRequests)),
				"Processing {Count} Rpc requests");

			responseFailedWithNoId = LoggerMessage.Define<int, string>(
				LogLevel.Error,
				new EventId(19, nameof(ResponseFailedWithNoId)),
				"Request with no id failed and no response will be sent. Error - Code: {Code}, Message: {Message}");

			noResponses = LoggerMessage.Define(
				LogLevel.Information,
				new EventId(20, nameof(NoResponses)),
				"No rpc responses created.");

			responses = LoggerMessage.Define<int>(
				LogLevel.Information,
				new EventId(21, nameof(Responses)),
				"{Count} rpc response(s) created.");

		}
		public static void LogException(this ILogger logger, Exception ex, string? message = null)
		{
			//Log error ignores the exception for some reason
			if (message != null)
			{
				message = $"{message}{Environment.NewLine}{ex}";
			}
			else
			{
				message = $"{ex}";
			}
			logger.LogError(new EventId(), ex, message);
		}

		public static void AttemptingToMatchMethod(this ILogger logger, string method)
		{
			LoggerExtensions.attemptingToMatchMethod(logger, method, null);
		}

		public static void RequestMatchedMethod(this ILogger logger)
		{
			LoggerExtensions.requestMatchedMethod(logger, null);
		}

		public static void MethodsInRoute(this ILogger logger, IEnumerable<IRpcMethodInfo> methods)
		{
			if (!logger.IsEnabled(LoggerExtensions.methodsInRouteLevel))
			{
				return;
			}
			string methodsString = string.Join(", ", methods.Select(m => m.Name));
			LoggerExtensions.methodsInRoute(logger, methodsString, null);
		}

		public static void InvokingBatchRequests(this ILogger logger, int count)
		{
			LoggerExtensions.invokingBatchRequests(logger, count, null);
		}

		public static void BatchRequestsComplete(this ILogger logger)
		{
			LoggerExtensions.batchRequestsComplete(logger, null);
		}
		public static void InvokingRequest(this ILogger logger, RpcId id)
		{
			LoggerExtensions.invokingRequest(logger, id, null);
		}
		public static void InvokeMethod(this ILogger logger, string method)
		{
			LoggerExtensions.invokeMethod(logger, method, null);
		}
		public static void InvokeMethodComplete(this ILogger logger, string method)
		{
			LoggerExtensions.invokeMethodComplete(logger, method, null);
		}
		public static void FinishedRequest(this ILogger logger, RpcId id)
		{
			LoggerExtensions.finishedRequest(logger, id, null);
		}
		public static void FinishedRequestNoId(this ILogger logger)
		{
			LoggerExtensions.finishedRequestNoId(logger, null);
		}
		public static void SkippingAuth(this ILogger logger)
		{
			LoggerExtensions.skippingAuth(logger, null);
		}
		public static void RunningAuth(this ILogger logger)
		{
			LoggerExtensions.runningAuth(logger, null);
		}
		public static void AuthSuccessful(this ILogger logger)
		{
			LoggerExtensions.authSuccessful(logger, null);
		}
		public static void AuthFailed(this ILogger logger)
		{
			LoggerExtensions.authFailed(logger, null);
		}
		public static void NoConfiguredAuth(this ILogger logger)
		{
			LoggerExtensions.noConfiguredAuth(logger, null);
		}
		public static void ParsingRequests(this ILogger logger)
		{
			LoggerExtensions.parsingRequests(logger, null);
		}
		public static void ParsedRequests(this ILogger logger, int count)
		{
			LoggerExtensions.parsedRequests(logger, count, null);
		}
		public static void ProcessingRequests(this ILogger logger, int count)
		{
			LoggerExtensions.processingRequests(logger, count, null);
		}
		public static void ResponseFailedWithNoId(this ILogger logger, int code, string message)
		{
			LoggerExtensions.responseFailedWithNoId(logger, code, message, null);
		}
		public static void NoResponses(this ILogger logger)
		{
			LoggerExtensions.noResponses(logger, null);
		}
		public static void Responses(this ILogger logger, int count)
		{
			LoggerExtensions.responses(logger, count, null);
		}

	}
}
