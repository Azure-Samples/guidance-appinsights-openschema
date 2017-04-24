// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Logging;

	public class MethodTraceMetric
	{
		/// <summary>
		/// Execute func and return elasped time in milliseconds
		///		duratation is logged as Metric if methodname provided
		/// </summary>
		/// <param name="func">func to execute</param>
		/// <param name="methodName">method name for metrics logging, if empty or null execute but no call to log metric</param>
		/// <param name="logger">ILogger for metrics extension call</param>
		/// <param name="fmt">format of vars params</param>
		/// <param name="vars">option params</param>
		/// <returns></returns>
		public static async Task<long> ExecuteTraceMetric(Func<Task> func, string methodName, ILogger logger, string fmt, params object[] vars)
		{
			long ms = Stopwatch.GetTimestamp();
			await func().ConfigureAwait(false);
			if (!String.IsNullOrEmpty(methodName))
			{
				// Convert GetTimeStamp to Milliseconds from units of 100 Nanoseconds
				ms = (Stopwatch.GetTimestamp() - ms) / 10000;
				logger.LogMetric(methodName, ms);
				return ms;
			}

			return -1;
		}

		/// <summary>
		/// Execute func and return type T of func
		///		duratation is logged as Metric if methodname provided
		/// </summary>
		/// <param name="func">func to execute</param>
		/// <param name="methodName">method name for metrics logging, if empty or null execute but no call to log metric</param>
		/// <param name="logger">ILogger for metrics extension call</param>
		/// <param name="fmt">format of vars params</param>
		/// <param name="vars">option params</param>
		/// <returns></returns>
		public static async Task<T> ExecuteTraceMetric<T>(Func<Task<T>> func, string methodName, ILogger logger, string fmt, params object[] vars)
		{
			T result = default(T);
			double ms = Stopwatch.GetTimestamp();
			result = await func().ConfigureAwait(false);
			if (!String.IsNullOrEmpty(methodName))
			{
				// Convert GetTimeStamp to Milliseconds from units of 100 Nanoseconds
				ms = (Stopwatch.GetTimestamp() - ms) / 10000.0;
				logger.LogMetric(methodName, (long)ms);
			}

			return result;
		}
	}
}