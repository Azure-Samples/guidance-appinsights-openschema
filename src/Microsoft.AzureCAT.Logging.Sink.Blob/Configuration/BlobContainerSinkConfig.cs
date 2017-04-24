// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;

	public class BlobSinkBufferConfig
	{
		public const string BlobSinkBufferSection = "BlobSinkBuffer";

		private const long MemoryBufferBytesDefault = 4*1024*1024;
		private const int BufferFlushDefault = 10;
		private const bool UseGzipDefault = false;

		public BlobSinkBufferConfig()
		{
			MemoryBufferBytes = MemoryBufferBytesDefault;
			BufferFlush = TimeSpan.FromSeconds(BufferFlushDefault);
			UseGzip = UseGzipDefault;
		}

		public long MemoryBufferBytes { get; set; }

		public TimeSpan BufferFlush { get; set; }

		public bool UseGzip { get; set; }
	}
}