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
	using Microsoft.WindowsAzure.Storage.Blob;
	using Microsoft.Extensions.Logging;

	public class AppInsightBlobSink 
		: BlobContainerSink<ITelemetry, IOutputBase>, ITelemetryProcessor
	{
		protected readonly ITransformOutput _transform;
		protected readonly string _dataSource;
		private readonly ITelemetryProcessor _next;

		public AppInsightBlobSink(
			ITelemetryProcessor next,
			IConfiguration config,
			IConfiguration schemaConfig,
			ILogger logger,
			IEnumerable<CloudBlobClient> blobClients,
			Func<CloudBlockBlob, Task> onBlobWrittenFunc,
			Func<string> containerNameFunc,
			string dataSource,
			string fileType)
			: base(config, logger, blobClients, onBlobWrittenFunc, containerNameFunc, dataSource, fileType)
		{
			_next = next;

			// Reflection: Create Instance of ITransformOpenSchema
			TransformConfig openSchemaConfig = new TransformConfig();
			schemaConfig.Bind(openSchemaConfig);
			var factoryType = Type.GetType(openSchemaConfig.TransformOutput.ClassAssembly, throwOnError: true);
			_transform = Activator.CreateInstance(factoryType) as ITransformOutput;
			_dataSource = dataSource;
		}

		public void Process(ITelemetry item)
		{
			// Filter applied in ProcessEntry
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
			if (item != null && _transform != null)
				return _transform.Filter(item, _dataSource);

			return false;
		}

		protected override IEnumerable<IOutputBase> Transform(IEnumerable<ITelemetry> items)
		{
			List<IOutputBase> eventList = new List<IOutputBase>();

			try
			{
				if (_transform != null)
				{
					foreach (var item in items)
					{
						var eventTelemetry = item as EventTelemetry;
						if (item == null) continue;

						var eventBlock = _transform.ToOutput(eventTelemetry, _dataSource);
						if (eventBlock != null)
							eventList.Add(eventBlock);
					}
				}
				else
				{
					_logger?.LogError($"{nameof(AppInsightBlobSink)}.{nameof(Transform)} MISSING transform function!");
				}
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, $"{nameof(AppInsightBlobSink)}.{nameof(Transform)} Error in publishing to blob storage");
			}

			return eventList;
		}

		protected override async Task Publish(IEnumerable<IOutputBase> items)
		{
			try
			{
				if (items == null)
				{
					_logger?.LogError($"{nameof(AppInsightBlobSink)}.{nameof(Publish)} null data");
					return;
				}

				foreach (var item in items)
				{
					byte[] byteEvent = _transform?.SerializeJSON(item);
					if (byteEvent != null)
						await WriteToBlobBuffer(byteEvent);
				}
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, $"{nameof(AppInsightBlobSink)}.{nameof(Publish)} exception publishing to blob storage");
			}
		}
	}
}