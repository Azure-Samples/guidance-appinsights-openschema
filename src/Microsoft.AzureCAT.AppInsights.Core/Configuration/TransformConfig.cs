// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	public class AssemblyInfoConfig
	{
		public string Name { get; set; }

		public string ClassAssembly { get; set; }
	}

	public class TransformConfig
	{
		public string Id { get; set; }

		public AssemblyInfoConfig TransformOutput { get; set; }
	}
}