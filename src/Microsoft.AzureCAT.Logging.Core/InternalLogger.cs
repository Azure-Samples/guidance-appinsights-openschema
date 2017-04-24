// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;
	using System.IO;
	using System.Diagnostics;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Configuration;

	public class InternalLogger
	{
		const string LogFileNamePrefix = "internallogger";
		const string LogFileType = "log";

		private static object _objLock = new object();

		public static ILoggerFactory Factory { get; private set; }

		public static ILogger Logger { get; private set; }


		/// <summary>
		/// Configuration options:
		///		LogLevel 
		///			"Console"
		///			"Debug"
		///			"LocalTraceFile"
		/// </summary>
		/// <param name="settingsPath"></param>
		/// <returns></returns>
		public static ILogger GetLogger(string settingsPath)
		{
			lock (_objLock)
			{
				// Read from Configuration for Internal Loggging
				IConfiguration config = DefaultConfiguration.LoadConfig(settingsPath, "appsettings.json");
				var logLevelSection = config.GetSection(LoggingSettingsConfig.LoggingSection).GetSection(LoggingSettingsConfig.LogLevelSection);
				LogLevel logLevelDefault = logLevelSection.GetValue<LogLevel>(LoggingSettingsConfig.LogLevelDefaultKey, LogLevel.None);
				// Use Default as the loglevel for the types we support
				LogLevel logLevelConsole = logLevelSection.GetValue<LogLevel>(LoggingSettingsConfig.LogLevelConsoleKey, logLevelDefault);
				LogLevel logLevelDebug = logLevelSection.GetValue<LogLevel>(LoggingSettingsConfig.LogLevelDebugKey, logLevelDefault);
				LogLevel logLevelLocalTraceFile = logLevelSection.GetValue<LogLevel>(LoggingSettingsConfig.LogLevelLocalTraceFileKey, logLevelDefault);

				// Logger Factory for Options
				Factory = new LoggerFactory();

				// Console, Debug
				if (logLevelConsole < LogLevel.None)
					Factory.AddConsole(logLevelConsole);
				if (logLevelDebug < LogLevel.None)
					Factory.AddDebug(logLevelDebug);

				// Write to Local File and Trace (DbgView)
				if (logLevelLocalTraceFile < LogLevel.None)
				{
					var fileSwitch = new SourceSwitch("fileSwitch");
					fileSwitch.Level = ConvertToTraceListenerLogLevel(logLevelLocalTraceFile); // SourceLevels.All;
					Factory.AddTraceSource(fileSwitch,
						new TextWriterTraceListener(File.Open(LocalFileName(), FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.Write)));
					Trace.AutoFlush = true;
				}

				// Create ILogger here
				Logger = Factory.CreateLogger<InternalLogger>();
			}

			return Logger;
		}

		public static string LocalFileName()
		{
			var date = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm");
			return $"{LogFileNamePrefix}-{date}.{LogFileType}";
		}

		public static SourceLevels ConvertToTraceListenerLogLevel(LogLevel logLevel)
		{
			switch (logLevel)
			{
				case LogLevel.Critical:
					return SourceLevels.Critical;
				case LogLevel.Error:
					return SourceLevels.Error;
				case LogLevel.Warning:
					return SourceLevels.Warning;
				case LogLevel.Information:
					return SourceLevels.Information;
				case LogLevel.Debug:
					return SourceLevels.Verbose;
				case LogLevel.Trace:
					return SourceLevels.Verbose;
				default:    // LogLevel.None
					return SourceLevels.Off;
			}
		}
	}
}