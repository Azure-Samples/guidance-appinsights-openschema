// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using ApplicationInsights.Extensibility.Implementation;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using System.Threading.Tasks;

	public interface ITelemetryProcessorSinkFactory
	{
		Task<bool> UseProcessorSink(IConfiguration config, ILogger logger, TelemetryProcessorChainBuilder aiClientBuilder);
	}
}