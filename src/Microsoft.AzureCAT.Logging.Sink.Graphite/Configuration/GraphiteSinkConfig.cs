// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;

	public class GraphiteSinkConfig
	{
		private const int PortDefault = 2003;
		private const int WindowSizeDefault = 15;
		private const int MaxWindowCountDefault = 100;

		public GraphiteSinkConfig()
		{
			Port = PortDefault;
			WindowSize = TimeSpan.FromSeconds(WindowSizeDefault);
			MaxWindowCount = MaxWindowCountDefault;
		}

		public string Server { get; set; }

		public int Port { get; set; }

		public TimeSpan WindowSize { get; set; }

		public int MaxWindowCount { get; set; }
	}
}