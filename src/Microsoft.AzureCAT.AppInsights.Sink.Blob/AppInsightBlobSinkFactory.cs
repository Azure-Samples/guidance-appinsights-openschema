// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;
	using System.Threading.Tasks;
	using ApplicationInsights.Extensibility.Implementation;
	using WindowsAzure.Storage.Blob;
	using WindowsAzure.Storage;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;

	public class AppInsightBlobSinkFactory : ITelemetryProcessorSinkFactory
	{
		public async Task<bool> UseProcessorSink(IConfiguration config, ILogger logger, TelemetryProcessorChainBuilder aiClientBuilder)
		{
			return await PublishOpenSchemaToBlobStorage(config, logger, aiClientBuilder);
		}

		private static async Task<bool> PublishOpenSchemaToBlobStorage(
			IConfiguration config,
			ILogger logger,
			TelemetryProcessorChainBuilder aiClientBuilder)
		{
			// AppInsights Config
			var appInsightsLoggingConfig = new AppInsightBlobConfig();
			config.Bind(appInsightsLoggingConfig);

			// Notification endpoint for Blobs to be read by AppInsights OpenSchema
			Uri blobNotificationEndpoint = null;
			if (!string.IsNullOrEmpty(appInsightsLoggingConfig.BlobNotificationEndpoint))
				blobNotificationEndpoint = new Uri(appInsightsLoggingConfig.BlobNotificationEndpoint);

			// Blob and OpenSchema Config
			var blobConfigSection = config.GetSection(AppInsightBlobConfig.BlobPublisherSection);
			List<string> storageAccountStrings = new List<string>();
			blobConfigSection.GetSection(AppInsightBlobConfig.StorageAccountsSection).Bind(storageAccountStrings);

			string fileType = blobConfigSection.GetValue<string>(AppInsightBlobConfig.FileTypeValue, AppInsightBlobConfig.JSONFileType);
			var baseContainerName = blobConfigSection.GetValue<string>(AppInsightBlobConfig.BaseContainerNameValue);

			List<CloudStorageAccount> storageAccounts = storageAccountStrings.Select(CloudStorageAccount.Parse).ToList();

			// Exit if no storage accounts in config
			if (storageAccounts.Count == 0)
			{
				logger?.LogWarning($"{nameof(PublishOpenSchemaToBlobStorage)} No Storage Accounts found in config.");
				return false;
			}

			var blobClients = storageAccounts.Select(s => s.CreateCloudBlobClient()).ToList();

			var schemaNameConfig = config.GetSection(AppInsightBlobConfig.SchemaIdListSection);
			foreach (var dataSourceSchemaId in schemaNameConfig.GetChildren())
			{
				string dataSourceName = dataSourceSchemaId.Key;
				TransformConfig openSchemaConfig = new TransformConfig();
				dataSourceSchemaId.Bind(openSchemaConfig);

				Func<CloudBlockBlob, Task> OpenSchemaCallbackFunc = async (blob) =>
				{
					await OpenSchemaCallback.PostCallback(
						logger: logger,
						blob: blob,
						endpoint: blobNotificationEndpoint,
						schemaName: openSchemaConfig.Id,
						iKey: appInsightsLoggingConfig.InstrumentationKey);
				};

				// Blob publisher
				aiClientBuilder.Use(
					(next) =>
						new AppInsightBlobSink(
							next: next,
							config: config,
							schemaConfig: dataSourceSchemaId,
							logger: logger,
							dataSource: dataSourceName,
							fileType: fileType,
							blobClients: blobClients,
							onBlobWrittenFunc: OpenSchemaCallbackFunc,
							// Use a naming function that promotes differences in the start of the name to improve storage paritioning
							containerNameFunc: () =>
							{
								var date = DateTime.UtcNow.ToString("yyyy-MM-dd-HH");
								return $"{GetHashPrefix(date, 5)}-{baseContainerName}-{date}";
							})
					);
			}

			return await Task.FromResult(true);
		}

		/// <summary>
		/// Helper function to build unique, partitioned blbo file name
		/// </summary>
		/// <param name="input"></param>
		/// <param name="prefixLength"></param>
		/// <returns></returns>
		private static string GetHashPrefix(string input, int prefixLength)
		{
			var md5 = System.Security.Cryptography.MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
			StringBuilder sb = new StringBuilder(prefixLength);
			if (prefixLength > hash.Length)
			{
				throw new ArgumentException("prefix length too long", nameof(prefixLength));
			}
			for (int i = 0; i < prefixLength; i++)
			{
				sb.Append(hash[i].ToString("x2"));
			}
			return sb.ToString();
		}
	}
}