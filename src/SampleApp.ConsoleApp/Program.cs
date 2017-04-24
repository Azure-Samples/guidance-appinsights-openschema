// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SampleApp.ConsoleApp
{
	using System;
	using System.IO;
	using System.Threading;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.AzureCAT.AppInsights;
	using Sample.OpenSchemas;

	public class Program
	{
		static ILogger _logger;
		static string _message = "SampleApp.ConsoleApp This is way too easy!";

		public static void Main(string[] args)
		{
			int iterationsLogs = 1;

			// How many interations?
			if (args.Length > 0)
				iterationsLogs = Convert.ToInt32(args[0]);

			#region OPTIONAL SETUP
			// OPTIONAL SETUP: Local Internal Logger loaded from config which may include Console, Debug, TraceFile
			ILogger localLogger = InternalLogger.GetLogger(Directory.GetCurrentDirectory());
			#endregion

			// Create Application Insights Logger
			string logLevelCategory = "Default";
			_logger = LoggingManager.ConfigureLogger(localLogger, Directory.GetCurrentDirectory(), logLevelCategory).GetAwaiter().GetResult();

			for (int loop = 0; loop < iterationsLogs; loop++)
			{
				Console.WriteLine();
				Console.WriteLine($">>>>> Run sample logging iteration {loop + 1} of {iterationsLogs} {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

				// Sample-1 - LogMetric: Aggregated Metrics: Graphite and AI customEvents (EventTelemetry)
				// >>> AggMetric EventTelemetry (customEvents) = Aggregated MetricTelemtry into EventTelemetry
				// >>> Graphite  = Aggregated MetricTelemtry into 9 event types (Avg, Min, Max, P99, etc.)
				_logger.LogMetric("TestMetricConsoleApp", 3);

				// Sample-2 - LogInformation: New Logger with Category and LogInformation, sends LogOpenSchema
				// >>> BlobSink LogOpenSchema
				// >>> ElasticSearch LogOpenSchema
				logLevelCategory = "SampleApp";
				var _loggerSampleApp = LoggingManager.GetLoggerFactory()?.CreateLogger(logLevelCategory);
				_loggerSampleApp.LogInformation(_message);
				Console.WriteLine(_message);

				// Sample-3 - TimedOperations: sends TimedOperationOpenSchema and sends LogMetric(MetricModel)
				// >>> BlobSink TimedOperationOpenSchema
				// >>> AggMetric EventTelemetry (customEvents) = Aggregated MetricTelemtry into EventTelemetry
				// >>> Graphite  = Aggregated MetricTelemtry into 9 event types (Avg, Min, Max, P99, etc.)
				using (_loggerSampleApp.BeginTimedOperation("SampleOp"))
				{
					Thread.Sleep(10);
				}

				// Sample-4 - LogError: sends ExceptionsOpenSchema
				// >>> BlobSink ExceptionsOpenSchema
				// >>> ElasticSearch ExceptionsOpenSchema
				try
				{
					throw new ArgumentException("A test exception!");
				}
				catch (Exception e)
				{
					_logger.LogError(0, e, "MyTest");
					Console.WriteLine($"Exception {e.Source} {e.Message}");
				}
			}

			Console.WriteLine($"Delay some as logs are sent on a timer (or when buffer is full)... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
			Console.WriteLine();
			Console.ReadKey();
		}
	}
}