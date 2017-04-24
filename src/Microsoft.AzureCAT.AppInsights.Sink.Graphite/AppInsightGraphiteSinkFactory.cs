// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using ApplicationInsights.Extensibility.Implementation;
	using System.Threading.Tasks;

	public class AppInsightGraphiteSinkFactory : ITelemetryProcessorSinkFactory
	{
		public Task<bool> UseProcessorSink(IConfiguration config, ILogger logger, TelemetryProcessorChainBuilder aiClientBuilder)
		{
			aiClientBuilder.Use((next) => new AppInsightGraphiteSink(
						next: next,
						logger: logger,
						config: config));

			return Task.FromResult<bool>(true);
		}
	}
}