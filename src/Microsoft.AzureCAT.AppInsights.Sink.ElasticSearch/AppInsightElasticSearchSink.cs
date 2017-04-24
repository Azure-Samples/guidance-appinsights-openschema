// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Collections.Generic;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public class AppInsightElasticSearchSink : ElasticSearchSink<ITelemetry>, ITelemetryProcessor
	{
		protected readonly ITelemetryProcessor _next;
		protected readonly ITransformOutput _transformOpenSchema;

		public AppInsightElasticSearchSink(
			ITelemetryProcessor next,
			IConfiguration config,
			ILogger logger)
			: base(config, logger, nameof(AppInsightElasticSearchSink))
		{
			_next = next;

			// Reflection: Create Instance of ITransformOpenSchema
			var openSchemaConfig = new AssemblyInfoConfig();
			config.GetSection(ElasticSearchSinkConfig.TransformOutputSection).Bind(openSchemaConfig);
			var factoryType = Type.GetType(openSchemaConfig.ClassAssembly, throwOnError: true);
			_transformOpenSchema = Activator.CreateInstance(factoryType) as ITransformOutput;
		}

		public void Process(ITelemetry item)
		{
			// Filter applied in PostEntry
			ProcessEntry(item);
			_next.Process(item);
		}

		/// <summary>
		/// Filter true means the ITelemetry event will be filtered out
		///        false means don't filter it out: process the event
		/// </summary>
		/// <param name="item">telemetry item to process</param>
		/// <returns></returns>
		protected override bool Filter(ITelemetry item)
		{
			var eventTelemetry = item as EventTelemetry;
			if (eventTelemetry != null && _transformOpenSchema != null)
			{
				return _transformOpenSchema.Filter(item);
			}

			return true;
		}

		protected override IList<object> Transform(IEnumerable<ITelemetry> items)
		{
			List<object> eventList = new List<object>();

			try
			{
				foreach (var item in items)
				{
					var eventTelemetry = item as EventTelemetry;
					if (eventTelemetry == null) continue;

					if (!Filter(item))
					{
						if (_transformOpenSchema != null)
						{
							var eventBlock = _transformOpenSchema.ToOutput(eventTelemetry);
							if (eventBlock != null)
								eventList.Add(eventBlock);
						}
						else
						{
							// No Transform Interface, just add EventTelemetry items
							eventList.Add(item);
						}
					}
					else
					{
						_logger?.LogDebug($"{nameof(AppInsightElasticSearchSink)}.{nameof(Transform)} item not set {item}");
					}
				}
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, $"{nameof(AppInsightElasticSearchSink)}.{nameof(Transform)} Error in publishing to blob storage");
			}

			return eventList;
		}
	}
}