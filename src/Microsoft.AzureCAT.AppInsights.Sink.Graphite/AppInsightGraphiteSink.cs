// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public class AppInsightGraphiteSink : GraphiteSink<ITelemetry>, ITelemetryProcessor
	{
		protected readonly ITelemetryProcessor _next;

		protected readonly AppInsightsGraphiteConfig graphiteTelemetryProperties = new AppInsightsGraphiteConfig();

		public AppInsightGraphiteSink(
			ITelemetryProcessor next,
			IConfiguration config,
			ILogger logger)
			: base(config, logger, nameof(AppInsightGraphiteSink))
		{
			_next = next;

			config.GetSection(AppInsightsGraphiteConfig.EventTelemetryPropertiesSection).Bind(graphiteTelemetryProperties.EventTelemetryProperties);
		}

		public void Process(ITelemetry item)
		{
			// Filter applied in PostEntry
			ProcessEntry(item);
			_next.Process(item);
		}

		protected override bool Filter(ITelemetry item)
		{
			// false means it's not filtered out so process it
			var metricTelemetry = item as MetricTelemetry;
			if (metricTelemetry != null)
			{
				// Aggregatate ALL MetricTelemetry that are marked for CustomPipeline
				if (metricTelemetry.Properties.ContainsKey(CoreConstants.CustomPipelineKey))
					return false;
			}

			return true;
		}

		protected override IList<string> Transform(IEnumerable<ITelemetry> evts)
		{
			var contentList = new List<string>();

			try
			{
				// Aggregate MetricTelemetry with same Metric "Name" 
				//	==> returns EventTelemetry which contains Metrics and Properties
				var eventTelemetry = MetricsAggregator.AggregateToEventTelemetry(evts);

				foreach (var e in eventTelemetry)
				{
					var me = e as EventTelemetry;
					if (me != null)
					{
                        // Build the metric name for Graphite using EventTelemetryProperties
                        // NOTE: properties are used in default order (specified in config file)
                        StringBuilder sbMetricName = new StringBuilder();
						foreach (var propName in graphiteTelemetryProperties.EventTelemetryProperties)
						{
							if (sbMetricName.Length > 0) sbMetricName.Append(".");
							sbMetricName.Append($"{me.GetProperty(propName, propName)}");
						}
						sbMetricName.Append($".{me.Name}");

						var metricName = GraphiteFormat(sbMetricName.ToString());

						contentList.Add(GraphiteEntry(metricName, MetricProps.Avg, $"{me.Metrics[MetricProps.Avg]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.Min, $"{me.Metrics[MetricProps.Min]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.Max, $"{me.Metrics[MetricProps.Max]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.Count, $"{me.Metrics[MetricProps.Count]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.StdDev, $"{me.Metrics[MetricProps.StdDev]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.P50, $"{me.Metrics[MetricProps.P50]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.P90, $"{me.Metrics[MetricProps.P90]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.P95, $"{me.Metrics[MetricProps.P95]}", me.Timestamp));
						contentList.Add(GraphiteEntry(metricName, MetricProps.P99, $"{me.Metrics[MetricProps.P99]}", me.Timestamp));
					}
				}
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, "AppInsightGraphiteSink.GeContent Failed");
			}

			return contentList;
		}
	}
}