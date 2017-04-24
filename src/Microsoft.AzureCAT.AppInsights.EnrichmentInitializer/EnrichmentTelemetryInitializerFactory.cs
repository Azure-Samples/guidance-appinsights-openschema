// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System.Threading.Tasks;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using System;

	public class EnrichmentTelemetryInitializerFactory : ITelemetryInitializerFactory
	{
		public Task<ITelemetryInitializer> CreateInitializer(IConfiguration config, ILogger logger, IServiceProvider serviceProvider)
		{
			return Task.FromResult<ITelemetryInitializer>(
				new EnrichmentTelemetryInitializer(config));
		}
	}
}