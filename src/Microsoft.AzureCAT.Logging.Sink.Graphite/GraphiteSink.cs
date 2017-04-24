// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net.Sockets;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public abstract class GraphiteSink<T> : BatchingPublisher<T>, IProcessorSink<T>
	{
		protected readonly GraphiteSinkConfig _graphiteConfig = new GraphiteSinkConfig();

		public GraphiteSink(
			IConfiguration config,
			ILogger logger,
			string name)
			: base(config, logger, name)
		{
			// Get Grpahite Config
			config.Bind(_graphiteConfig);
		}

		public virtual void ProcessEntry(T evt)
		{
			base.PostEntry(evt);
		}

		protected abstract IList<string> Transform(IEnumerable<T> evts);

		protected override async Task Publish(IEnumerable<T> evts)
		{
			if (evts == null)
				return;
			try
			{
				var eventsList = evts.ToArray();
				if (eventsList.Length == 0)
					return;

				var content = Transform(eventsList);
				await PublishGraphite(content);
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, "Error Publishing GraphiteSink events");
			}
		}

		protected async Task PublishGraphite(IList<string> events)
		{
			if (events == null || events.Count == 0)
				return;

			using (var tcpClient = new TcpClient())
			{
				await tcpClient.ConnectAsync(_graphiteConfig.Server, _graphiteConfig.Port);
				using (var stream = tcpClient.GetStream())
				using (var sw = new StreamWriter(stream))
				{
					foreach (var e in events)
					{
						await sw.WriteLineAsync(e);
					}
				}
			}

			_logger?.LogDebug($"{nameof(GraphiteSink<int>)}.{nameof(PublishGraphite)} {events.Count()} events to graphite");
		}

		protected virtual string GraphiteFormat(string graphiteString)
		{
			return graphiteString
				.ToLower()
				.Replace(' ', '_')
				.Replace(':', '.')
				.Replace('/', '_')
				.Replace("\"", "")
				.Replace("%", "")
				.TrimStart('.')
				.TrimEnd('.')
				.TrimEnd('\n');
		}

		protected virtual string GraphiteEntry(string name, string nameAppend, string value, DateTimeOffset timestamp)
		{
			return $"{name}.{nameAppend.ToLower()} {value} {timestamp.ToUnixTimeSeconds()}";
		}
	}
}