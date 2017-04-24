// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System.Collections.Generic;

	public class AppInsightsGraphiteConfig
	{
		public const string EventTelemetryPropertiesSection = "EventTelemetryProperties";

		public AppInsightsGraphiteConfig()
		{
			EventTelemetryProperties = new List<string>();
		}

		public List<string> EventTelemetryProperties { get; set; }
	}
}