// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	public class AppInsightBlobConfig
	{
		public const string BlobPublisherSection = "BlobPublisher";
		public const string StorageAccountsSection = "StorageAccounts";

		public const string SchemaIdListSection = "SchemaIdList";

		public const string BaseContainerNameValue = "BaseContainerName";
		public const string FileTypeValue = "FileType";

		public const string JSONFileType = "json";

		public const string BlobNotificationEndpointDefault = "https://dc.services.visualstudio.com/v2/track";


		// Properties
		public string InstrumentationKey { get; set; }
		public string BlobNotificationEndpoint { get; set; }

		public AppInsightBlobConfig()
		{
			BlobNotificationEndpoint = BlobNotificationEndpointDefault;
		}
	}
}