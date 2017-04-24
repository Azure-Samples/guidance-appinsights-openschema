// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using Nest;

	public abstract class ElasticSearchSink<T> : BatchingPublisher<T>, IProcessorSink<T>
	{
		protected ElasticSearchSinkConfig _elasticSearchConfig = new ElasticSearchSinkConfig();

		private readonly ElasticClient _elasticClient;

		public ElasticSearchSink(
			IConfiguration config,
			ILogger logger,
			string name)
			: base(config, logger, name)
		{
			// Get Elastic Search Config
			config.Bind(_elasticSearchConfig);

			Uri elasticSearchUrl = new Uri($"{_elasticSearchConfig.ElasticSearchUrl}:{_elasticSearchConfig.Port}", UriKind.Absolute);

			var connectionSettings = new ConnectionSettings(elasticSearchUrl)
				.RequestTimeout(TimeSpan.FromSeconds(_elasticSearchConfig.TimeoutSeconds))
				.MaximumRetries(_elasticSearchConfig.MaximumRetries);

			if (!string.IsNullOrEmpty(_elasticSearchConfig.UserName))
				connectionSettings.BasicAuthentication(_elasticSearchConfig.UserName, _elasticSearchConfig.Password);

			// ElasticSearch Client
			_elasticClient = new ElasticClient(connectionSettings);
		}

		public virtual void ProcessEntry(T evt)
		{
			base.PostEntry(evt);
		}

		protected abstract IList<object> Transform(IEnumerable<T> events);

		protected override async Task Publish(IEnumerable<T> evts)
		{
			if (evts == null)
				return;
			try
			{
				var content = Transform(evts);
				await PublishElasticSearch(content);
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, "Error Publishing ElasticSearchSink events");
			}
		}

		protected async Task PublishElasticSearch(IEnumerable<object> evts)
		{
			try
			{
				List<object> objectList = evts.ToList();
				if (!objectList.Any())
					return;

				string indexName = $"{_elasticSearchConfig.IndexNameBase}-{DateTime.UtcNow.ToString("MMddyyyy")}";
                //string type = objectList.First().GetType().Name;
                string type = _elasticSearchConfig.TypeName;
                IBulkResponse rsp = await _elasticClient.IndexManyAsync(objectList, indexName, type);
				if (rsp.Errors)
					_logger?.LogError("Bulk Errors {0}", rsp.Errors.ToString());
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, "ElasticSearch PublishEvents error");
			}

			_logger?.LogDebug("Published {ElasticSearchEventCount} events to ElasticSearch", evts.Count());
		}
	}
}