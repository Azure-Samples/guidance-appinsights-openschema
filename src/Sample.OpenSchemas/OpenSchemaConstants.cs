// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
    using System.Collections.Generic;
    using Microsoft.AzureCAT.AppInsights;

    /// <summary>
    /// LogType is class schema name where each class has schema implementation
    /// </summary>
    public static class LogTypes
    {
        public const string Log = "LogOpenSchema";
        public const string Exceptions = "ExceptionsOpenSchema";
        public const string TimedOperation = "TimedOperationOpenSchema";
    }

    public static class Constants
    {
        public const string ExceptionMessage = "ExceptionMessage";
        public const string ExceptionOccurrenceId = "ExceptionOccurrenceId";
        public const string ExceptionStackTrace = "ExceptionStackTrace";
        public const string ExceptionType = "ExceptionType";
        public const string ExceptionDepth = "ExceptionDepth";

        public const string ExceptionFormatString =
            "ExceptionOccurrenceId={ExceptionOccurrenceId}, ExceptionType={ExceptionType}, " +
            "ExceptionMessage={ExceptionMessage}, ExceptionStackTrace={ExceptionStackTrace}, " +
            "ExceptionDepth={ExceptionDepth}";


        public static Dictionary<string, string> OpenSchemaMappings = new Dictionary<string, string>
        {
            {OpenSchemaProps.MessageName, CoreConstants.OpenSchemaNameKey},
            {OpenSchemaProps.CorrelationId, TelemetryProps.CorrelationIdKey},
            {OpenSchemaProps.SecondaryCorrelationId, TelemetryProps.SecondaryCorrelationIdKey},
            {OpenSchemaProps.Level, TelemetryProps.Level},
            {OpenSchemaProps.Environment, TelemetryProps.Environment},
            {OpenSchemaProps.MachineRole, TelemetryProps.MachineRole},
            {OpenSchemaProps.MachineName, TelemetryProps.MachineName},
            {OpenSchemaProps.ApplicationName, TelemetryProps.CategoryName},
            {OpenSchemaProps.Message, TelemetryProps.FormattedMessage},
            {OpenSchemaProps.MessageTemplate, TelemetryProps.MessageTemplate},
        };
    }

    public static class OpenSchemaProps
		{
			public const string CorrelationId = "CorrelationId";
			public const string SecondaryCorrelationId = "SecondaryCorrelationId";
			public const string Level = "Level";
			public const string Environment = "Environment";
			public const string MachineRole = "MachineRole";
			public const string MachineName = "MachineName";
			public const string ApplicationName = "ApplicationName";
			public const string MessageName = "MessageName";
			public const string Message = "Message";
			public const string MessageTemplate = "MessageTemplate";
			public const string UnknownMessage = "UnknownMessage";
			public const string DurationInMs = "DurationInMs";
			public const string StartTime = "StartTime";
			public const string EndTime = "EndTime";
			public const string OperationId = "OperationId";
		}

    public static class TelemetryProps
    {
        public const string CorrelationIdKey = "CorrelationId";
        public const string SecondaryCorrelationIdKey = "SecondaryCorrelationId";

        public const string CategoryName = "CategoryName";
		public const string Level = "Level";

		public const string FormattedMessage = "FormattedMessage";
		public const string UnknownFormat = "???";
		public const string Id = "Id";
		public const string Name = "Name";

		public const string Environment = "Environment";
		public const string MachineRole = "MachineRole";
		public const string MachineName = "MachineName";

		public const string MessageTemplate = "MessageTemplate";
		public const string CustomPropertyPrefix = "customProperty___";
		public const string MetricAggregation = "MetricAggregation";
	}
}