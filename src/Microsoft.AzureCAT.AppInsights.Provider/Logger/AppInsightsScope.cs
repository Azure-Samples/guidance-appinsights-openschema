// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;

	public class AppInsightsScope : IDisposable
	{
		private readonly AppInsightsLoggerProvider _provider;
		private readonly object _state;

		public AppInsightsScope(AppInsightsLoggerProvider provider, object state)
		{
			_provider = provider;
			_state = state;
		}

		public AppInsightsScope Parent { get; }

		public void Dispose()
		{
		}
	}
}