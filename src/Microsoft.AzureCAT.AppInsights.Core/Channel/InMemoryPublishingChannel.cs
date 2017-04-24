// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility.Implementation;
	using Microsoft.AzureCAT.Logging;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Replacement for the standard AI publishing channel using TPL data flow for flow control
	/// and concurrency management
	/// </summary>
	public class InMemoryPublishingChannel : BatchingPublisher<ITelemetry>, ITelemetryChannel
	{
		protected readonly string _bypassPipelineKey;

		private volatile bool _developerMode = false;

		public InMemoryPublishingChannel(
			IConfiguration config, 
			ILogger logger,
			Uri endpointAddress, 
			string bypassPipelineKey)
			: base(config, logger, nameof(InMemoryPublishingChannel))
		{
			_endpointAddress = endpointAddress;
			_bypassPipelineKey = bypassPipelineKey;
		}

		/// <summary>
		/// Serializes a list of telemetry items and sends them to Application Insights
		/// </summary>
		protected override async Task Publish(IEnumerable<ITelemetry> items)
		{
			try
			{
				if (items == null || !items.Any())
					return;

				byte[] data = JsonSerializer.Serialize(items);
				var transmission = new Transmission(
					_endpointAddress,
					data,
					"application/x-json-stream",
					JsonSerializer.CompressionType);

				await transmission.SendAsync().ConfigureAwait(false);

				_logger?.LogDebug($"{nameof(InMemoryPublishingChannel)}.{nameof(Publish)} {items.Count()} events to AppInsights");
			}
			catch (Exception e)
			{
				_logger?.LogError(0, $"{nameof(InMemoryPublishingChannel)}.{nameof(Publish)}", e);
			}
		}

		protected override bool Filter(ITelemetry item)
		{
			// AI Publisher will ignore Telemetry items containing _bypassPipelineKey Property
			var itemProperties = item as ISupportProperties;
			return itemProperties != null 
				&& itemProperties.Properties.ContainsKey(_bypassPipelineKey);
		}

		#region ITelemetryChannelImpl

		/// <summary>
		/// ITelemetryChannel interface method
		/// </summary>
		public bool? DeveloperMode
		{
			get { return _developerMode; }
			set
			{
				if (value.HasValue)
					_developerMode = value.Value;
				else
					_developerMode = false;
			}
		}

		protected Uri _endpointAddress { get; set; }

		public string EndpointAddress
		{
			get { return _endpointAddress.ToString(); }
			set { _endpointAddress = new Uri(value); }
		}

		public void Flush()
		{
			base.Flush();
		}

		public void Send(ITelemetry item)
		{
			PostEntry(item);
		}

		#endregion
	}
}