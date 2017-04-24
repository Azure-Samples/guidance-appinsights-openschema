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
	public abstract class BaseOpenSchemaWithMessage : BaseOpenSchema
	{
		// Message can be large, make it one of the last properties
		[JsonProperty(Order = 200)]
		public string Message { get; set; }
		[JsonProperty(Order = 11)]
		public string MessageTemplate { get; set; }
	}
}