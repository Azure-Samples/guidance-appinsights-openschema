// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.Extensions.Configuration;

	/// <summary>
	/// Configuration Telemetry Initialization: read configuration settings for log enrichment properties
	/// </summary>
	public class EnrichmentTelemetryInitializer : ITelemetryInitializer
	{
		private readonly IConfigurationSection _logEnrichmentSection;

		public EnrichmentTelemetryInitializer(IConfiguration config)
		{
			if (config == null)
			{
				throw new ArgumentNullException(nameof(config));
			}
			_logEnrichmentSection = config.
				GetSection(EnrichmentConfig.EnrichmentPropertiesSection);
		}

		public void Initialize(ITelemetry item)
		{
			// Supported Application Insights types
			if (item is EventTelemetry || item is MetricTelemetry)
			{
				var eventTelemetry = item as ISupportProperties;
				if (eventTelemetry == null) return;

				// Hard coded enrichment: always use MachineName
				eventTelemetry.Properties[EnrichmentConfig.MachineNamePropertyKey] = Environment.MachineName;

				foreach (var prop in _logEnrichmentSection.GetChildren())
				{
					eventTelemetry.Properties[prop.Key] = prop.Value;
				}
			}
		}
	}
}