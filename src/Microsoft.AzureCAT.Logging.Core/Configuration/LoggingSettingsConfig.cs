// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	public class LoggingSettingsConfig
	{
		public const string LoggingSection = "Logging";
		public const string LogLevelSection = "LogLevel";
		public const string IncludeScopesSection = "IncludeScopes";

		public const string LogLevelDefaultKey = "Default";
		public const string LogLevelConsoleKey = "Console";
		public const string LogLevelDebugKey = "Debug";
		public const string LogLevelLocalTraceFileKey = "LocalTraceFile";
	}
}