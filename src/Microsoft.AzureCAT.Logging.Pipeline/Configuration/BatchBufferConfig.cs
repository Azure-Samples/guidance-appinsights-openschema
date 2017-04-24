// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;

	public class BatchBufferConfig
	{
		public const string BatchBufferConfigSection = "BatchBuffer";

		private const int MaxBacklogDefault = 1000;
		private const int MaxWindowCountDefault = 1000;
		private const int WindowSizeDefault = 10;

		public BatchBufferConfig()
		{
			MaxBacklog = MaxBacklogDefault;
			MaxWindowCount = MaxWindowCountDefault;
			WindowSize = TimeSpan.FromSeconds(WindowSizeDefault);
		}

		public int MaxBacklog { get; set; }

		public int MaxWindowCount { get; set; }

		public TimeSpan WindowSize { get; set; }
	}
}