// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Threading;
	using Microsoft.ApplicationInsights;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public class AppInsightsLoggerProvider : ILoggerProvider
	{
		protected readonly IConfiguration _config;
		protected readonly ILogger _logger;

		readonly AsyncLocal<AppInsightsScope> _value = new AsyncLocal<AppInsightsScope>();

		private ImmutableDictionary<string, LogLevel> _levels;
		private LogLevel _defaultLevel = LogLevel.Error;

		public AppInsightsLoggerProvider(
			IConfiguration config,
			ILogger logger)
		{
			_config = config;
			_logger = logger;

			Client = new TelemetryClient();

			var appInsightsLoggingConfig = new AppInsightsLoggingConfig();
			config.GetSection(AppInsightsLoggingConfig.ApplicationInsightsSection).Bind(appInsightsLoggingConfig);
			Client.InstrumentationKey = appInsightsLoggingConfig.InstrumentationKey;

			// Load in the level map
			var change = config.GetReloadToken();
			change.RegisterChangeCallback(ConfigUpdated, null);

			LoadConfiguration(_logger);
		}

		public AppInsightsScope CurrentScope
		{
			get { return _value.Value; }
			set { _value.Value = value; }
		}

		public TelemetryClient Client { get; }

		public IPopulateTelemetry PopulateLogTelemetry { get; private set; }

		public void Dispose()
		{
		}

		public bool IsEnabled(string categoryName, LogLevel level)
		{
			if (_levels.ContainsKey(categoryName))
				return level >= _levels[categoryName];
			else
				return level >= _defaultLevel;
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new AppInsightsLogger(this, categoryName);
		}

		public IDisposable BeginScope<T>(T state)
		{
			return new AppInsightsScope(this, state);
		}

		private void ConfigUpdated(object obj)
		{
			LoadConfiguration(_logger);
		}

		private void LoadConfiguration(ILogger logger)
		{
			// Populate Logging info to ITelemetry
			var populateTypeConfig = new AssemblyInfoConfig();
			var populateTelemetrySection = _config.GetSection(AppInsightsLoggingConfig.ApplicationInsightsSection)
				.GetSection(AppInsightsLoggingConfig.PopulateTelemetry);
			foreach (var populateItem in populateTelemetrySection.GetChildren())
				populateItem.Bind(populateTypeConfig);

			// Using Reflection, get the IPopulateTelemetry function
			var factoryType = Type.GetType(populateTypeConfig.ClassAssembly, throwOnError: true);
			PopulateLogTelemetry = Activator.CreateInstance(factoryType) as IPopulateTelemetry;

			// Read Logging Levels
			var levels = _config.GetSection(AppInsightsLoggingConfig.ApplicationInsightsSection)?
				.GetSection(LoggingSettingsConfig.LogLevelSection);

			var dictLogLevel = new Dictionary<string, LogLevel>();
			LogLevel defaultLevel = LogLevel.Warning;

			if (levels != null)
			{
				foreach (var k in levels.GetChildren())
				{
					LogLevel level;
					if (Enum.TryParse(k.Value, true, out level))
						dictLogLevel.Add(k.Key, level);
					else
						dictLogLevel.Add(k.Key, LogLevel.Warning);
				}

				if (dictLogLevel.ContainsKey("Default"))
				{
					defaultLevel = dictLogLevel["Default"];
					dictLogLevel.Remove("Default");
				}
			}

			_levels = dictLogLevel.ToImmutableDictionary();
			_defaultLevel = defaultLevel;
		}
	}
}