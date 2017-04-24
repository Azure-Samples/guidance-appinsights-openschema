// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using Newtonsoft.Json;
	using System;

	/// <summary>
	/// OpenSchema are custom schemas sent to AppIngights
	/// Json ordering is critical for error case when overall size is too big
	///  but only first 1000 bytes are written as error
	/// </summary>
	public class TimedOperationOpenSchema : BaseOpenSchemaWithMessage
	{
		[JsonProperty(Order = 100)]
		public DateTime StartTime { get; set; }
		[JsonProperty(Order = 101)]
		public DateTime EndTime { get; set; }
		[JsonProperty(Order = 102)]
		public int DurationInMs { get; set; }
		[JsonProperty(Order = 103)]
		public string OperationId { get; set; }
	}
}