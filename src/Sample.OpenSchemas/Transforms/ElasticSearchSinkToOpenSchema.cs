// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using System;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AzureCAT.AppInsights;

 	public class ElasticSearchSinkToOpenSchema : ITransformOutput
	{
        // For Elasticsearch, just use the BlobSink open schemas and transforms
		private readonly BlobSinkToOpenSchema _blobSinkToOpenSchema;

        // Process only these schemas, future TODO: add to config
        private const string DefaultSchemaFilters = "LogOpenSchema|ExceptionsOpenSchema";

        public ElasticSearchSinkToOpenSchema()
		{
			_blobSinkToOpenSchema = new BlobSinkToOpenSchema();
		}

		public byte[] SerializeJSON(IOutputBase evt)
		{
			throw new NotImplementedException();
		}

		public byte[] SerializeCSV(IOutputBase evt)
		{
			throw new NotImplementedException();
		}

		public bool Filter(ITelemetry evt, string schemaName)
		{
			var eventTelemetry = evt as EventTelemetry;
			if (eventTelemetry != null)
			{
				if (string.IsNullOrEmpty(schemaName))
					schemaName = DefaultSchemaFilters;

				if (eventTelemetry.Properties.ContainsKey(CoreConstants.OpenSchemaNameKey)
				    && schemaName.Contains(eventTelemetry.Properties[CoreConstants.OpenSchemaNameKey]))
					return false;
			}

			return true;
		}

		public IOutputBase ToOutput(EventTelemetry eventTelemetry, string dataSourceName = "")
		{
			if (string.IsNullOrEmpty(dataSourceName))
				dataSourceName = eventTelemetry.Properties[CoreConstants.OpenSchemaNameKey];

			if (eventTelemetry.Properties.ContainsKey(TelemetryProps.CategoryName)
				&& eventTelemetry.Properties.ContainsKey(TelemetryProps.Level))
			{
				return _blobSinkToOpenSchema.ToOutput(eventTelemetry, dataSourceName);
			}

			return null;
		}
	}
}