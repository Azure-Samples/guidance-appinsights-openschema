// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public static class AppInsightsLoggerFactoryExtensions
	{
		public static ILoggerFactory AddAppInsights(
			this ILoggerFactory factory,
			IConfiguration config,
			ILogger logger = null,
			IEnumerable<ITelemetryInitializer> telemetryInitializers = null,
			bool dispose = false)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			factory.AddProvider(new AppInsightsLoggerProvider(config, logger));

			return factory;
		}
	}
}