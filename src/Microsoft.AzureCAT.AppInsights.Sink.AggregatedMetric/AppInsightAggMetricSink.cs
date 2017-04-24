// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public class AppInsightAggMetricSink 
		: BatchingPublisherTransform<ITelemetry, ITelemetry>, ITelemetryProcessor
	{
		private readonly ITelemetryProcessor _next;

		public AppInsightAggMetricSink(
			ITelemetryProcessor next,
			IConfiguration config,
			ILogger logger)
			: base(config: config,
				logger: logger,
				name: nameof(AppInsightAggMetricSink))
		{
			_next = next;
		}

		/// <summary>
		/// Process item in our Aggregator then send item to next processor
		/// </summary>
		/// <param name="item">Event to aggregate</param>
		public void Process(ITelemetry item)
		{
			// Filter applied in PostEntry
			PostEntry(item);
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

		protected override IEnumerable<ITelemetry> Transform(IEnumerable<ITelemetry> items)
		{
			try
			{
                return MetricsAggregator.AggregateToEventTelemetry(items);
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, $"{nameof(AppInsightAggMetricSink)}.{nameof(Transform)} Failed");
			}

			return null;
		}

		protected override async Task Publish(IEnumerable<ITelemetry> items)
		{
			try
			{
				int itemCount = 0;
				foreach (var item in items)
				{
                    // Send to AppInsights endpoint via ImMemoryPublisher
                    var e = item as EventTelemetry;
					if (e != null)
					{
                        // NOTE: Remove Custom Properties to enable Send to InMemory Publisher to AppInsights
                        //       CustomPipelineKey used in InMemory Publisher FILTER
                        if (e.Properties.ContainsKey(CoreConstants.CustomPipelineKey))
							e.Properties.Remove(CoreConstants.CustomPipelineKey);

						// InMemory Publisher will send to AppInsights (EventTelemetry==customEvents or MetricTelemetry==customMetrics)
						TelemetryConfiguration.Active.TelemetryChannel.Send(item);

						itemCount++;
					}
					else
					{
						_logger?.LogError($"{nameof(AppInsightAggMetricSink)}.{nameof(Publish)} TelemetryProcessor Invalid EventTelemetry of type {item.GetType()}");
					}
				}

				_logger?.LogDebug($"{nameof(AppInsightAggMetricSink)}.{nameof(Publish)} {itemCount} AggMetric events to AppInsights");
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, $"{nameof(AppInsightAggMetricSink)}.{nameof(Publish)} exception publishing");
			}

			await Task.FromResult(0);
		}
	}
}