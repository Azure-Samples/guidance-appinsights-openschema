// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using Microsoft.AzureCAT.AppInsights;
	using Microsoft.Extensions.Logging;
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using static Sample.OpenSchemas.Constants;

	public static class OpenSchemaLoggingExtensions
	{
		static readonly EventId EventId = new EventId(0, null);

		public static void LogException(this ILogger logger, Exception exception, string message, params object[] args)
		{
			Guid occurrenceId = Guid.NewGuid();

			logger.LogException(exception, occurrenceId, 1, message, args);
		}

		public static void LogMetric(this ILogger logger, string metricName, long metricValue)
		{
			if (logger == null)
			{
				throw new ArgumentNullException(nameof(logger));
			}

			MetricModel logModel = new MetricModel
			{
				Name = metricName,
				Value = metricValue
			};

			logger.Log(LogLevel.Information, EventId, logModel, null, (schema, exception) => null);
		}

		public static IDisposable BeginTimedOperation(this ILogger logger, string operationId)
		{
			return new TimedOperation(logger, operationId);
		}

		#region private
		private static void LogException(this ILogger logger, Exception exception, Guid occurrenceId, int depth, string message, params object[] args)
		{
			if (depth > 10)
			{
				logger.LogInformation("LogException depth greater than 10 for {OccurrenceId}, giving up logging inner exception(s).", occurrenceId);
				return;
			}

			var eid = new EventId(0, LogTypes.Exceptions);

			string exceptionType = exception.GetType().Name;
			string exceptionMessage = exception.Message;
			string exceptionStackTrace = exception.StackTrace;

			// Combine argument list
			args = args.Concat(new List<object> { occurrenceId, exceptionType, exceptionMessage, exceptionStackTrace, depth }).ToArray();

			// Combine formatted log message
			string formattedMessage = $"{message} [{ExceptionFormatString}]";

			logger.LogError(eid, formattedMessage, args);

			var agg = exception as AggregateException;
			if (agg != null)
			{
				var flattened = agg.Flatten();
				foreach (var innerException in flattened.InnerExceptions)
				{
					logger.LogException(innerException, occurrenceId, depth + 1, message, args);
				}
			}

			if (exception.InnerException != null)
			{
				logger.LogException(exception.InnerException, occurrenceId, depth + 1, message, args);
			}
		}
		#endregion
	}

	internal class TimedOperation : IDisposable
	{
		private readonly string _operationId;
		private readonly ILogger _logger;
		private readonly DateTime _startTime;
        // These fields in the FormatString match the property names in our schema TimedOperationOpenSchema
        private const string FormatString = "Operation {OperationId} finished executing in {DurationInMs} ms, StartTime: {StartTime}, EndTime {EndTime}";

		public TimedOperation(ILogger logger, string operationId)
		{
			_logger = logger;
			_operationId = operationId;
			_startTime = DateTime.UtcNow;
		}

		#region IDisposable Support
		private bool _disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				var endTime = DateTime.UtcNow;
				EventId eid = new EventId(0, LogTypes.TimedOperation);
				var startTimeStr = _startTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
				var endTimeStr = endTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
				TimeSpan span = endTime - _startTime;
				int durationInMs = (int)span.TotalMilliseconds;
				_logger.LogInformation(eid, FormatString, _operationId, durationInMs, startTimeStr, endTimeStr);
				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}