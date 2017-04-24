// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AzureCAT.AppInsights;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using static Sample.OpenSchemas.Constants;

    /// <summary>
    /// Mapping from EventTelemetry to OpenSchema
    /// </summary>
	public static class OpenSchemaExtensions
	{
		public static void PopulateEventBlock(this BaseOpenSchema eventBlock, 
			EventTelemetry eventTelemetry, 
			Dictionary<string, object> customProperties = null)
		{
			var mapping = OpenSchemaMappings;

			eventBlock.CorrelationId = eventTelemetry.GetProperty(mapping[OpenSchemaProps.CorrelationId],
												   eventTelemetry.Context.Operation.CorrelationVector);
			eventBlock.SecondaryCorrelationId = eventTelemetry.GetProperty(mapping[OpenSchemaProps.SecondaryCorrelationId],
															eventTelemetry.Context.Operation.ParentId);
			eventBlock.Timestamp = eventTelemetry.Timestamp.UtcDateTime;
			eventBlock.MessageId = eventTelemetry.Context.Operation.Id;
			eventBlock.Level = eventTelemetry.GetProperty(mapping[OpenSchemaProps.Level]);

			eventBlock.Environment = eventTelemetry.GetProperty(mapping[OpenSchemaProps.Environment],
												 eventTelemetry.Context.Device.OperatingSystem);
			eventBlock.MachineRole = eventTelemetry.GetProperty(mapping[OpenSchemaProps.MachineRole],
												 eventTelemetry.Context.Cloud.RoleInstance);
			eventBlock.MachineName = eventTelemetry.GetProperty(mapping[OpenSchemaProps.MachineName],
												 eventTelemetry.Context.Cloud.RoleName);

			eventBlock.ApplicationName = eventTelemetry.GetProperty(mapping[OpenSchemaProps.ApplicationName]);
			eventBlock.MessageName = eventTelemetry.GetProperty(mapping[OpenSchemaProps.MessageName]);
			eventBlock.MessageId = Guid.NewGuid().ToString();
			if (customProperties != null)
			{
				eventBlock.Blob = JsonConvert.SerializeObject(customProperties);
			}
		}

		public static void PopulateMessageEvent(this BaseOpenSchemaWithMessage messageTelemetry,
			EventTelemetry eventTelemetry)
		{
            var mapping = OpenSchemaMappings;

            messageTelemetry.MessageTemplate = eventTelemetry.GetProperty(mapping[OpenSchemaProps.MessageTemplate]);
			messageTelemetry.Message = eventTelemetry.GetProperty(mapping[OpenSchemaProps.Message]);
		}

		public static void PopulateLogEvent(this LogOpenSchema logTelemetry,
			EventTelemetry eventTelemetry)
		{
            var mapping = OpenSchemaMappings;

            if (eventTelemetry.GetProperty(OpenSchemaProps.UnknownMessage) != null)
				logTelemetry.UnknownMessage = Convert.ToBoolean(eventTelemetry.GetProperty(mapping[OpenSchemaProps.UnknownMessage]));
		}

		public static void PopulateExceptionsEvent(this ExceptionsOpenSchema exceptionEvent,
			EventTelemetry eventTelemetry)
		{
			exceptionEvent.ExceptionDepth = Convert.ToInt32(eventTelemetry.GetProperty(ExceptionDepth));
			exceptionEvent.ExceptionMessage = eventTelemetry.GetProperty(ExceptionMessage);
			exceptionEvent.ExceptionOccurrenceId = eventTelemetry.GetProperty(ExceptionOccurrenceId);
			exceptionEvent.ExceptionStackTrace = eventTelemetry.GetProperty(ExceptionStackTrace);
			exceptionEvent.ExceptionType = eventTelemetry.GetProperty(ExceptionType);
		}

		public static void PopulateTimedOperationEvent(this TimedOperationOpenSchema timedopEvent,
			EventTelemetry eventTelemetry)
		{
			if (eventTelemetry.GetProperty(OpenSchemaProps.StartTime) != null)
			{
				timedopEvent.StartTime = Convert.ToDateTime(eventTelemetry.GetProperty(OpenSchemaProps.StartTime));
			}
			if (eventTelemetry.GetProperty(OpenSchemaProps.EndTime) != null)
			{
				timedopEvent.EndTime = Convert.ToDateTime(eventTelemetry.GetProperty(OpenSchemaProps.EndTime));
			}
			if (eventTelemetry.GetProperty(OpenSchemaProps.DurationInMs) != null)
			{
				timedopEvent.DurationInMs = Convert.ToInt32(eventTelemetry.GetProperty(OpenSchemaProps.DurationInMs));
			}

			timedopEvent.OperationId = eventTelemetry.GetProperty(OpenSchemaProps.OperationId);
		}
	}
}
