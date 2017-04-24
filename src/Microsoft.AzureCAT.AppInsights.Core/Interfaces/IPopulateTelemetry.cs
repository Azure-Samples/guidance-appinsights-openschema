// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.Extensions.Logging;

	public interface IPopulateTelemetry
	{
		ITelemetry FormatTelemetry<TState>(
			string category, LogLevel logLevel, EventId eventId, TState state,
			Exception exception, Func<TState, Exception, string> formatter);
	}
}