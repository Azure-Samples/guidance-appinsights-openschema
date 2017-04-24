// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AzureCAT.AppInsights;
	using Microsoft.Extensions.Logging;
	using static Sample.OpenSchemas.Constants;

	public class PopulateTelemetry : IPopulateTelemetry
	{
		public const string OriginalFormatKey = "{OriginalFormat}";

        // TODO: make configurable, more discoverable instead of specifying here (Attributes?)
		private const string LogTypesNamespace = "Sample.OpenSchemas";
		private const string LogTypesClass = "LogTypes";    // Defined public static class LogTypes 

        // datetime format for Application Insights OpenSchema (only supports 3 digits of ms resolution)
        private const string DateTimeFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK";

        // Dictionary for each OpenSchema class with their Property names
        private readonly Dictionary<Type, HashSet<string>> _openSchemaProperties = new Dictionary<Type, HashSet<string>>();

		public PopulateTelemetry()
		{
			PopulateSchemaProperties();
		}

		/// <summary>
		/// Create Dictionaries of All OpenSchema classes with their Property names
		/// </summary>
		private void PopulateSchemaProperties()
		{
			// Get the Log Schemas from the Constants class
			var logTypesFullName = $"{LogTypesNamespace}.{LogTypesClass}";
			var logSchemas = Type.GetType(logTypesFullName);

			if (logSchemas == null)
				return;

			var schemaClassNames = new List<string>();

			// Pull the OpenSchema class names
			foreach (var field in logSchemas.GetFields())
			{
				var className = (string)field.GetValue(null);
				schemaClassNames.Add(className);
			}

            // Pull properties for each OpenSchema class
			foreach (var className in schemaClassNames)
			{
				var fullName = $"{LogTypesNamespace}.{className}";
                var schemaType = Type.GetType(fullName);
				if (schemaType == null)
					continue;
				var propNames = new HashSet<string>();
				foreach (var property in schemaType.GetProperties())
				{
					propNames.Add(property.Name);
				}
				_openSchemaProperties.Add(schemaType, propNames);
			}
		}

		public ITelemetry FormatTelemetry<TState>(
			string category, LogLevel logLevel, EventId eventId, TState state,
			Exception exception, Func<TState, Exception, string> formatter)
		{
			ITelemetry telemetryItem = null;

			// Create EventTelemetry for Log state to put through AI pipeline
			ISupportProperties telemetryProperties = null;

			// Aggregated Metrics
			if (state is MetricModel)
			{
				telemetryItem = new MetricTelemetry();
			    telemetryProperties = (ISupportProperties)telemetryItem;

                var metric = state as MetricModel;
				var metricModel = telemetryItem as MetricTelemetry;
				metricModel.Name = metric.Name;
				metricModel.Value = metric.Value;
				telemetryItem.Timestamp = DateTimeOffset.UtcNow;
				// Bypass AI Client SDK Pipeline
				telemetryProperties.Properties.Add(CoreConstants.CustomPipelineKey, "true");
				telemetryProperties.Properties.Add(TelemetryProps.MetricAggregation, "true");
			}
			else
			{   // OpenSchema
				telemetryItem = new EventTelemetry(category);
				telemetryProperties = (ISupportProperties)telemetryItem;

				// Bypass AI Client SDK Pipeline
				telemetryProperties.Properties.Add(CoreConstants.CustomPipelineKey, "true");

				// Set Basic Properties
				telemetryItem.Timestamp = DateTimeOffset.UtcNow;
				telemetryProperties.Properties.Add(TelemetryProps.CategoryName, category);
				telemetryProperties.Properties.Add(TelemetryProps.Level, logLevel.ToString());

				// Get default schema type
				var className = LogTypes.Log;
				if (eventId.Name != null)
					className = eventId.Name;
				var fullName = $"{LogTypesNamespace}.{className}";
				var schemaType = Type.GetType(fullName);

				// OpenSchema - classname
				telemetryProperties.Properties.Add(CoreConstants.OpenSchemaNameKey, className);

				string messageTemplate = null;
				var structure = state as IEnumerable<KeyValuePair<string, object>>;
				if (structure != null)
				{
					foreach (var property in structure)
					{
						// Plain "printf" style log message (no embedded structure)
						if (property.Key == OriginalFormatKey
							&& property.Value is string)
						{
							messageTemplate = (string)property.Value;

							if (exception != null)
								messageTemplate = $"{messageTemplate} {ExceptionFormatString}";
						}
						// If the schemas dictionary contains this schema type, check if the property is a first level property of the schema
						else if (schemaType != null && _openSchemaProperties.ContainsKey(schemaType)
							&& _openSchemaProperties[schemaType].Contains(property.Key))
						{
							telemetryProperties.Properties.Add(property.Key, property.Value?.ToString());
						}
						else
						{
							// If it is datetime or datetimeoffset we need to make sure we serialize it 
                            // in the standard way so OpenSchema can read it back and parse as datetime
							string stringValue = null;

							Type propertyType = property.Value?.GetType();
							if (propertyType == typeof(DateTime))
							{
								stringValue = ((DateTime)property.Value).ToString(DateTimeFormatString);
							}
							else if (propertyType == typeof(DateTimeOffset))
							{
								// OpenSchema doesn't currently support DateTimeOffset so we need to log as datetime
								// but make sure we turn into UTC first before the ToString() to make it the right format
								// something like 2017-05-18T00:54:28.004Z
								stringValue = ((DateTimeOffset)property.Value).UtcDateTime.ToString(DateTimeFormatString);
							}
							else
							{
								stringValue = property.Value?.ToString();
							}
							telemetryProperties.Properties.Add(
								TelemetryProps.CustomPropertyPrefix + property.Key, 
								stringValue);
						}
					}

					// If exception was passed add additional properties to the state and message template
					if (exception != null)
					{
						// Change schema from Default "Log" to "Exception"
						telemetryProperties.Properties[CoreConstants.OpenSchemaNameKey] = LogTypes.Exceptions;

						// STILL need to map OpenSchema "name" for transform
						if (!telemetryProperties.Properties.ContainsKey(ExceptionOccurrenceId))
							telemetryProperties.Properties.Add(ExceptionOccurrenceId, null);

						if (!telemetryProperties.Properties.ContainsKey(ExceptionType))
							telemetryProperties.Properties.Add(ExceptionType, exception.GetType().Name);

						if (!telemetryProperties.Properties.ContainsKey(ExceptionMessage))
							telemetryProperties.Properties.Add(ExceptionMessage, exception.Message);

						if (!telemetryProperties.Properties.ContainsKey(ExceptionStackTrace))
							telemetryProperties.Properties.Add(ExceptionStackTrace, exception.StackTrace);

						if (!telemetryProperties.Properties.ContainsKey(ExceptionDepth))
							telemetryProperties.Properties.Add(ExceptionDepth, 1.ToString());
					}

					string formattedState = formatter(state, null);
					telemetryProperties.Properties.Add(TelemetryProps.FormattedMessage, formattedState);
				}
				else
				{
					// Unknown
					telemetryProperties.Properties.Add(TelemetryProps.FormattedMessage, TelemetryProps.UnknownFormat);
				}

				// Map in the event id and name
				if (eventId.Id != 0)
					telemetryProperties.Properties.Add(TelemetryProps.Id, eventId.Id.ToString());
				if (eventId.Name != null)
					telemetryProperties.Properties.Add(TelemetryProps.Name, eventId.Name);
				// Add the message template
				if (messageTemplate != null)
					telemetryProperties.Properties.Add(TelemetryProps.MessageTemplate, messageTemplate);
			}

			return telemetryItem;
		}
	}
}