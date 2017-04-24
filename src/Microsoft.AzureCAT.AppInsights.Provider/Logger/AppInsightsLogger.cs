// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using Microsoft.ApplicationInsights;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.Extensions.Logging;

	public class AppInsightsLogger : ILogger
	{
		private const string DefaultCategoryName = "Default";
		private readonly TelemetryClient _telemetryClient;
		private readonly AppInsightsLoggerProvider _provider;
		private readonly IPopulateTelemetry _transform;
		private readonly string _categoryName;


		public AppInsightsLogger(
			AppInsightsLoggerProvider provider,
			string categoryName = null)
		{
			_provider = provider;
			_transform = provider.PopulateLogTelemetry;
			_telemetryClient = provider.Client;
			_categoryName = categoryName ?? DefaultCategoryName;
		}

		public void Log<TState>(
			LogLevel logLevel, EventId eventId, TState state,
			Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!_provider.IsEnabled(_categoryName, logLevel))
				return;

			// Populate ITelemetry through Reflection FormatTelemetry
			ITelemetry telemetryModel = _transform?.FormatTelemetry<TState>(_categoryName, logLevel, eventId, state, exception, formatter);

			// Push these ITelemetry types into the publishing pipeline
			if (telemetryModel is EventTelemetry)
			{
				_telemetryClient.TrackEvent(telemetryModel as EventTelemetry);
			}
			else if (telemetryModel is MetricTelemetry)
			{
				_telemetryClient.TrackMetric(telemetryModel as MetricTelemetry);
			}
			else
			{
				throw new ArgumentException($"{nameof(AppInsightsLogger)}.{nameof(Log)} Invalid State object type");
			}
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return _provider.IsEnabled(_categoryName, logLevel);
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return _provider.BeginScope(state);
		}
	}
}