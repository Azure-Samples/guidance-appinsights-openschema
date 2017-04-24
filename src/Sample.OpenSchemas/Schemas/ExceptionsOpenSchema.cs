// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Sample.OpenSchemas
{
	using Newtonsoft.Json;

	/// <summary>
	/// OpenSchema are custom schemas sent to AppIngights
	/// Json ordering is critical for error case when overall size is too big
	///  but only first 1000 bytes are written as error
	/// </summary>
	public class ExceptionsOpenSchema : BaseOpenSchemaWithMessage
	{
		[JsonProperty(Order = 100)]
		public string ExceptionOccurrenceId { get; set; }
		[JsonProperty(Order = 101)]
		public string ExceptionType { get; set; }
		[JsonProperty(Order = 201)]
		// ExceptionMessage can be large, make it one of the last properties
		public string ExceptionMessage { get; set; }
		// ExceptionStackTrace can be large, make it one of the last properties
		[JsonProperty(Order = 202)]
		public string ExceptionStackTrace { get; set; }
		[JsonProperty(Order = 102)]
		public int ExceptionDepth { get; set; }
	}
}