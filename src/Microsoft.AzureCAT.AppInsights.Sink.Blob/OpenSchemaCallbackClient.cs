// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.AppInsights
{
	using System;
	using System.Diagnostics;
	using System.Net.Http;
	using System.Threading.Tasks;
	using Microsoft.WindowsAzure.Storage.Blob;
	using Newtonsoft.Json.Linq;
	using Microsoft.Extensions.Logging;

	public class OpenSchemaCallback
	{
		protected static readonly Lazy<HttpClient> _httpclient = new Lazy<HttpClient>(
			() =>
			{
				HttpClient httpclient = new HttpClient();
				httpclient.DefaultRequestHeaders.Clear();
				return httpclient;
			});

		protected static HttpClient httpClient
		{
			get { return _httpclient.Value; }
		}

		public static async Task PostCallback(
			ILogger logger,
			CloudBlockBlob blob,
			Uri endpoint,
			string schemaName,
			string iKey)
		{
			try
			{
				var sasPolicy = new SharedAccessBlobPolicy();
				sasPolicy.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
				sasPolicy.Permissions = SharedAccessBlobPermissions.Read;
				var sasToken = blob.GetSharedAccessSignature(sasPolicy);
				var sasUri = blob.Uri + sasToken;

				var payload = new JObject(
					new JProperty(
						"data",
						new JObject(
							new JProperty("baseType", "OpenSchemaData"),
							new JProperty(
								"baseData",
								new JObject(
									new JProperty("ver", "2"),
									new JProperty("blobSasUri", sasUri),
									new JProperty("sourceName", schemaName),
									new JProperty("sourceVersion", "1.0")
									)
								)
							)
						),
					new JProperty("ver", "1"),
					new JProperty("name", "Microsoft.ApplicationInsights.OpenSchema"),
					new JProperty("time", DateTime.UtcNow),
					new JProperty("iKey", iKey)
					);

				var sw = Stopwatch.StartNew();
				HttpResponseMessage rsp = await httpClient
					.PostAsync(endpoint, new StringContent(payload.ToString()))
					.ConfigureAwait(false);

				logger?.LogTrace("OpenSchemaCallback publish result {StatusCode} took {ElapsedTimeInMs}", rsp.StatusCode, sw.ElapsedMilliseconds);
			}
			catch (Exception e)
			{
				// TODO: Handles errors 400, 403 and 404: https://docs.microsoft.com/en-us/azure/application-insights/app-insights-analytics-import
				// Error responses
				// 400 bad request: indicates that the request payload is invalid. Check:
				//		Correct instrumentation key.
				//		Valid time value.It should be the time now in UTC.
				//		Data conforms to the schema.
				// 403 Forbidden: The blob you've sent is not accessible. Make sure that the shared access key is valid and has not expired.
				// 404 Not Found:
				//		The blob doesn't exist.
				//		The data source name is wrong.
				logger?.LogError(0, e, "OpenSchemaCallback failed to publish callback with error {ExceptionMessage}", e.Message);
			}
		}
	}
}