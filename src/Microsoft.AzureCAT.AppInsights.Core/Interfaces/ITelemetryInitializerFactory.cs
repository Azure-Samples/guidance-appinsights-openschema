// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using ApplicationInsights.Extensibility;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.DependencyInjection;
	using System;
	using System.Threading.Tasks;

	public interface ITelemetryInitializerFactory
	{
		Task<ITelemetryInitializer> CreateInitializer(IConfiguration config, ILogger logger, IServiceProvider serviceProvider);
	}
}