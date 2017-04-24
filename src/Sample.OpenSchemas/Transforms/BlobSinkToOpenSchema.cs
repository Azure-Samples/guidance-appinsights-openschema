// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AzureCAT.AppInsights;
	using Newtonsoft.Json;
	using static Constants;

	public class BlobSinkToOpenSchema : ITransformOutput
	{
		public byte[] SerializeJSON(IOutputBase evt)
		{
			return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evt));
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
				if (eventTelemetry.Properties.ContainsKey(CoreConstants.OpenSchemaNameKey)
				    && eventTelemetry.Properties[CoreConstants.OpenSchemaNameKey] == schemaName)
					return false;
			}

			return true;
		}

		public IOutputBase ToOutput(EventTelemetry eventTelemetry, string schemaName)
		{
			IOutputBase eventBlock = null;

			// Assume Filter has already been applied!
			if (!string.IsNullOrEmpty(schemaName))
			{
				switch (schemaName)
				{
                    case LogTypes.Log:
                        eventBlock = new LogOpenSchema();
						break;
					case LogTypes.Exceptions:
						eventBlock = new ExceptionsOpenSchema();
						break;
					case LogTypes.TimedOperation:
						eventBlock = new TimedOperationOpenSchema();
						break;
					default:
						throw new ArgumentException("Unknown OpenSchema Type");
				}

				Dictionary<string, object> customProperties = new Dictionary<string, object>();
				foreach (string property in eventTelemetry.Properties.Keys)
				{
					if (property.StartsWith(TelemetryProps.CustomPropertyPrefix))
					{
						customProperties.Add(property.Remove(0, TelemetryProps.CustomPropertyPrefix.Length), eventTelemetry.Properties[property]);
					}
				}

				// BaseEvent Properties
				var baseEvent = eventBlock as BaseOpenSchema;
				baseEvent.PopulateEventBlock(eventTelemetry, customProperties);
				// WithMessage Properties
				var messageEvent = eventBlock as BaseOpenSchemaWithMessage;
				messageEvent?.PopulateMessageEvent(eventTelemetry);

				var logEvent = eventBlock as LogOpenSchema;
				if (logEvent != null)
				{
					logEvent.PopulateLogEvent(eventTelemetry);
					return eventBlock;
				}
				var exceptionEvent = eventBlock as ExceptionsOpenSchema;
				if (exceptionEvent != null)
				{
					exceptionEvent.PopulateExceptionsEvent(eventTelemetry);
					return eventBlock;
				}
				var timedOperationEvent = eventBlock as TimedOperationOpenSchema;
				if (timedOperationEvent != null)
				{
					timedOperationEvent.PopulateTimedOperationEvent(eventTelemetry);
					return eventBlock;
				}
			}

			return eventBlock;
		}
	}
}