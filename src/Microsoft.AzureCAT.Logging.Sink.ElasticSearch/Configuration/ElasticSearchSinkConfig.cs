// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
    using System;

    public class ElasticSearchSinkConfig
	{
		public const string TransformOutputSection = "TransformOutput";

        private const string DefaultIndexBase = "esbase";
        private const string DefaultTypeName = "estypename";
        private const int TimeoutSecondsDefault = 30;
		private const int MaximumRetriesDefault = 5;
		private const int PortDefault = 9202;
		private const int WindowSizeDefault = 30;
		private const int MaxWindowCountDefault = 100;

		public ElasticSearchSinkConfig()
		{
            IndexNameBase = DefaultIndexBase;
            TypeName = DefaultTypeName;
            TimeoutSeconds = TimeoutSecondsDefault;
			MaximumRetries = MaximumRetriesDefault;
			Port = PortDefault;
			WindowSize = TimeSpan.FromSeconds(WindowSizeDefault);
			MaxWindowCount = MaxWindowCountDefault;
		}

		public string ElasticSearchUrl { get; set; }

        public string IndexNameBase { get; set; }

        public string TypeName { get; set; }

        public string UserName { get; set; }

		public string Password { get; set; }

		public int TimeoutSeconds { get; set; }

		public int MaximumRetries { get; set; }

		public int Port { get; set; }

		public TimeSpan WindowSize { get; set; }

		public int MaxWindowCount { get; set; }
	}
}