// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.Extensibility;

	/// <summary>
	/// Called at startup to initialize ILogger and build the Application Insights Pipeline
	/// </summary>
	public static class LoggingManager
	{
        const string DefaultConfigFileName = "appsettings.json";
        const string DefaultCategoryName = "Default";

        private static ILoggerFactory _factory;
		private static ILogger _logger;

		public static ILoggerFactory GetLoggerFactory()
		{
			return _factory;
		}

		public static ILogger GetLogger()
		{
			return _logger;
		}

		public static async Task<ILogger> ConfigureLogger(ILogger logger, string settingsPath, string categoryName, IServiceProvider serviceProvider = null)
		{
			var config = DefaultConfiguration.LoadConfig(settingsPath, DefaultConfigFileName);

			await Initialize(config, logger, serviceProvider);

			_factory = new LoggerFactory().AddAppInsights(config);
			_logger = _factory.CreateLogger(string.IsNullOrEmpty(categoryName) ? DefaultCategoryName : categoryName);

			return _logger;
		}

		/// <summary>
		/// Initialize AppInsights pipeline: read config and enable sinks for logging
		/// </summary>
		/// <param name="config"></param>
		/// <param name="initaInitializers"></param>
		/// <returns></returns>
		public static async Task Initialize(IConfiguration config, ILogger logger, IServiceProvider serviceProvider)
		{
			// AppInsights Config
			var configAppInsights = config.GetSection(AppInsightsLoggingConfig.ApplicationInsightsSection);
			var appInsightsLoggingConfig = new AppInsightsLoggingConfig();
			configAppInsights.Bind(appInsightsLoggingConfig);

			// >>>>> Use the custom in-memory pipeline publishing channel
			// Config for InMemoryPublishingChannel
			var configInMemoryPublishingChannel = configAppInsights
				.GetSection(AppInsightsLoggingConfig.InMemoryPublishingChannelSection);
			TelemetryConfiguration.Active.TelemetryChannel =
				new InMemoryPublishingChannel(
					configInMemoryPublishingChannel,
					logger,
					new Uri(appInsightsLoggingConfig.TelemetryServiceEndpoint),
					CoreConstants.CustomPipelineKey);

			// >>>>> Add each ITelemetryInitializer loaded from config via Reflection and calling Factory for ITelemetryInitializer
			var telemetryInitializerSection = configAppInsights.GetSection(AppInsightsLoggingConfig.TelemetryInitializer);
			foreach (var initializerEntry in telemetryInitializerSection.GetChildren())
			{
				var initializerEntryConfig = new AssemblyInfoConfig();
				initializerEntry.Bind(initializerEntryConfig);

				ITelemetryInitializerFactory factory;
				try
				{
					// Using Reflection, get the ITelemetryInitializerFactory interface
					var factoryType = Type.GetType(initializerEntryConfig.ClassAssembly, throwOnError: true);
					factory = Activator.CreateInstance(factoryType) as ITelemetryInitializerFactory;
					ITelemetryInitializer telemetryInitializer = await factory.CreateInitializer(initializerEntry, logger, serviceProvider);

					TelemetryConfiguration.Active.TelemetryInitializers.Add(telemetryInitializer);
				}
				catch (Exception e)
				{
					logger?.LogError(0, e, nameof(Initialize));
				}
			}

			// >>>>> Set up a custom app insights pipeline: add custom sinks
			var aiClientBuilder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;

			// Get list from Config of IProcessorSinks, add them to the TelemetryProcessorChainBuilder
			var processorSinksSection = configAppInsights.GetSection(AppInsightsLoggingConfig.TelemetryProcessorSinks);
			foreach (var sinkSection in processorSinksSection.GetChildren())
			{
				var sinkConfig = new AssemblyInfoConfig();
				sinkSection.Bind(sinkConfig);

				ITelemetryProcessorSinkFactory factory;
				try
				{
					// Using Reflection, get the ITelemetryProcessorSinkFactory interface
					var factoryType = Type.GetType(sinkConfig.ClassAssembly, throwOnError: true);
					factory = Activator.CreateInstance(factoryType) as ITelemetryProcessorSinkFactory;
					await factory.UseProcessorSink(sinkSection, logger, aiClientBuilder);
				}
				catch (Exception e)
				{
					logger?.LogError(0, e, nameof(Initialize));
				}
			}

			// Update the ai client configuration
			aiClientBuilder.Build();
		}

		/// <summary>
		/// WORKAROUND fix: AI Client SDK pipeline will truncate proprties to 8KB,
		///		Post telemetry items directly to sink implementing IProcessorSink
		///		which will bypass AI Client SDK pipeline
		/// </summary>
		/// <param name="item"></param>
		public static void PostEntryToProcessors(ITelemetry item)
		{
			foreach (var tp in TelemetryConfiguration.Active.TelemetryProcessors)
			{
				var processorSink = tp as IProcessorSink<ITelemetry>;
				processorSink?.ProcessEntry(item);
			}
		}
	}
}