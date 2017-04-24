// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using Microsoft.Extensions.Configuration;

	public class DefaultConfiguration
	{
		public static IConfigurationRoot LoadConfig(string settingsPath, string fileName)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(settingsPath)
				.AddJsonFile(fileName, optional: true, reloadOnChange: false);
			return builder.Build();
		}
	}
}