// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using MathNet.Numerics.Statistics;

    public static class MetricsAggregator
    {
        /// <summary>
        /// Aggregates multiple MetricTelemetry objects into EventTelemetry grouped by Name
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<ITelemetry> AggregateToEventTelemetry(IEnumerable<ITelemetry> items)
        {
            var metrics = items
                .OfType<MetricTelemetry>()
                .GroupBy(e => new { e.Name })
                .Select(
                    e => new EventTelemetryCollection()
                    {
                        Event = new EventTelemetry()
                        {
                            Name = e.Key.Name,
                            Timestamp = e.Min(t => t.Timestamp)
                        },
                        // Copy properties
                        Properties = new Dictionary<string, string>(e.FirstOrDefault().Properties),
                        InstrumentationKey = e.First().Context.InstrumentationKey,
                        // Use the merge method to pull the percentiles into the
                        // proprties dictionary                     
                        Metrics = new Dictionary<string, double>()
                        {
                            {MetricProps.Avg, e.Average(t => t.Sum)},
                            {MetricProps.Min, e.Min(t => t.Sum)},
                            {MetricProps.Max, e.Max(t => t.Sum)},
                            {MetricProps.Count, e.Count()},
                            {MetricProps.StdDev, e.Select(t => t.Sum).StandardDeviation()},
                            {MetricProps.P50, e.Select(t => t.Sum).Percentile(50)},
                            {MetricProps.P90, e.Select(t => t.Sum).Percentile(90)},
                            {MetricProps.P95, e.Select(t => t.Sum).Percentile(95)},
                            {MetricProps.P99, e.Select(t => t.Sum).Percentile(99)}
                        }
                    })
                .Select(e => e.Merge());

            return metrics;
        }

        /// <summary>
        /// Aggregates multiple MetricTelemetry objects into 1 MetricTelemetry per Name (aggregated/grouped per Name)
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<ITelemetry> AggregateToMetricTelemetry(IEnumerable<ITelemetry> items)
        {
            var metrics = items
                .OfType<MetricTelemetry>()
                .GroupBy(e => new { e.Name })
                .Select(
                    e => new MetricTelemetryCollection()
                    {
                        Metric = new MetricTelemetry()
                        {
                            Name = e.Key.Name,
                            Timestamp = e.Min(t => t.Timestamp),
                            Sum = e.Average(t => t.Sum),
                            Min = e.Min(t => t.Sum),
                            Max = e.Max(t => t.Sum),
                            Count = e.Count(),
                            StandardDeviation = e.Select(t => t.Sum).StandardDeviation()
                        },
                        // Copy properties
                        Properties = new Dictionary<string, string>(e.FirstOrDefault().Properties),
                        InstrumentationKey = e.First().Context.InstrumentationKey,
                        // Use the merge method to pull the percentiles into the
                        // proprties dictionary                     
                        CustomMetrics = new Dictionary<string, double>()
                        {
                            {MetricProps.Avg, e.Average(t => t.Sum)},
                            {MetricProps.Min, e.Min(t => t.Sum)},
                            {MetricProps.Max, e.Max(t => t.Sum)},
                            {MetricProps.Count, e.Count()},
                            {MetricProps.StdDev, e.Select(t => t.Sum).StandardDeviation()},
                            {MetricProps.P50, e.Select(t => t.Sum).Percentile(50)},
                            {MetricProps.P90, e.Select(t => t.Sum).Percentile(90)},
                            {MetricProps.P95, e.Select(t => t.Sum).Percentile(95)},
                            {MetricProps.P99, e.Select(t => t.Sum).Percentile(99)}
                        }
                    })
                .Select(e => e.Merge());

            return metrics;
        }
    }

    public class EventTelemetryCollection
    {
        public string InstrumentationKey { get; set; }

        public EventTelemetry Event { get; set; }

        public IDictionary<string, double> Metrics { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public EventTelemetry Merge()
        {
            Event.Context.InstrumentationKey = InstrumentationKey;
            foreach (var nr in Metrics)
            {
                if (!Event.Metrics.ContainsKey(nr.Key))
                    Event.Metrics.Add(nr.Key, nr.Value);
            }
            foreach (var nr in Properties)
            {
                if (!Event.Properties.ContainsKey(nr.Key))
                    Event.Properties.Add(nr.Key, nr.Value);
            }

            return Event;
        }
    }

    public class MetricTelemetryCollection
    {
        public string InstrumentationKey { get; set; }

        public MetricTelemetry Metric { get; set; }

        public IDictionary<string, double> CustomMetrics { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public MetricTelemetry Merge()
        {
            Metric.Context.InstrumentationKey = InstrumentationKey;
            foreach (var nr in Properties)
            {
                if (!Metric.Properties.ContainsKey(nr.Key))
                    Metric.Properties.Add(nr.Key, nr.Value);
            }
            foreach (var nr in CustomMetrics)
            {
                if (!Metric.Properties.ContainsKey(nr.Key))
                    Metric.Properties.Add(nr.Key, nr.Value.ToString());
            }

            return Metric;
        }
    }

    public static class MetricProps
    {
        public const string Avg = "Avg";
        public const string Min = "Min";
        public const string Max = "Max";
        public const string Count = "Count";
        public const string StdDev = "StdDev";
        public const string P50 = "P50";
        public const string P90 = "P90";
        public const string P95 = "P95";
        public const string P99 = "P99";
    }
}