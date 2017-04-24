// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using Microsoft.ApplicationInsights.DataContracts;

	public static class TelemetryExtensions
	{
		public static string GetProperty(this ISupportProperties propertiesTelemetry, string key, string defaultValue = null)
		{
			if (propertiesTelemetry.Properties.ContainsKey(key))
			{
				return propertiesTelemetry.Properties[key];
			}
			return defaultValue;
		}
	}
}