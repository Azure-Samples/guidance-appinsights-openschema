// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using System;
	using Microsoft.AzureCAT.AppInsights;
	using Newtonsoft.Json;

	/// <summary>
	/// OpenSchema are custom schemas sent to AppIngights
	/// Json ordering is critical for error case when overall size is too big
	///  but only first 1000 bytes are written as error
	/// </summary>
	public class BaseOpenSchema : IOutputBase
	{
		[JsonProperty(Order = 1)]
		public string CorrelationId { get; set; }
		[JsonProperty(Order = 2)]
		public string SecondaryCorrelationId { get; set; }
		[JsonProperty(Order = 3)]
		public DateTime Timestamp { get; set; }
		[JsonProperty(Order = 4)]
		public string MessageId { get; set; }
		[JsonProperty(Order = 5)]
		public string Level { get; set; }
		[JsonProperty(Order = 6)]
		public string Environment { get; set; }
		[JsonProperty(Order = 7)]
		public string MachineRole { get; set; }
		[JsonProperty(Order = 8)]
		public string MachineName { get; set; }
		[JsonProperty(Order = 9)]
		public string ApplicationName { get; set; }
		[JsonProperty(Order = 10)]
		public string MessageName { get; set; }

		// Blob can be large, make it one of the last properties
		[JsonProperty(Order = 301)]
		public string Blob { get; set; }
	}
}