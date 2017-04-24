// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	public class AppInsightsLoggingConfig
	{
		public const string ApplicationInsightsSection = "ApplicationInsights";
		public const string InMemoryPublishingChannelSection = "InMemoryPublishingChannel";

        public const string PopulateTelemetry = "PopulateTelemetry";                // List of PopulateTelemetry impl: IPopulateTelemetryFactory
        public const string TelemetryInitializer = "TelemetryInitializer";          // List of ITelemetryInitializer impl: ITelemetryInitializerFactory
		public const string TelemetryProcessorSinks = "TelemetryProcessorSinks";    // List of ITelemetryProcessor impl: ITelemetryProcessorSinkFactory
		
		
		// Properties
		public string InstrumentationKey { get; set; }

		public string TelemetryServiceEndpoint { get; set; }
	}
}