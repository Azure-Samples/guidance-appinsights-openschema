// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;

	public interface ITransformOutput
	{
		byte[] SerializeJSON(IOutputBase evt);
		byte[] SerializeCSV(IOutputBase evt);
		bool Filter(ITelemetry evt, string schemaName = "");
		IOutputBase ToOutput(EventTelemetry eventTelemetry, string schemaName = "");
	}
}