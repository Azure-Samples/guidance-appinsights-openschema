---
services: application-insights
platforms: dotnet
author: michthub
date: May, 2017
---

# Application Insights Analytics with OpenSchema
This sample shows how to implement an Azure Monitoring and Analytics solution at large scale with Application Insights Client SDK pipeline leveraging Application Insights Analytics with OpenSchema and 3rd party services for real-enough-time dashboards.

## Sample Applications
The Visual Studio solution provides two sample console applications.
1. SampleApp.ConsoleAppCore: .NET Core App
2. SampleApp.ConsoleApp: .NET Framework App

Both console apps reference the Sample.OpenSchemas assembly which shows how to implement custom schemas with Application Insights OpenSchema and leverage the custom extensions in the Microsoft.AzureCAT namespaces.

## Building the sample
- The solution is built with Visual Studio 2015 and requires the .NET Core SDK. Modify global.json for the version you install.
- Open the .sln solution file in Visual Studio 2015.
- Select configuraiton solution platform of x64, not All CPU.
- Build All.
- If the SampleApp.ConsoleApp errors, just Build again to get it to resolve assemblies. This is a known issue with combined use of .xproj and .csproj. If references need to be updated, the .csproj will have to be manually edited when referencing any .xproj.
- Copy the appsettings.json.clean to appsettings.json, then replace each {{Secret-Key}} with your values. Specifying these values assumes you have created an Application Insights application which will give you an Instrumentation Key.
- See below for further [Configuration](#configuration) details.

## Microsoft.Extensions.Logging - ILogger
Logging uses the Microsoft logging extensions to provide the base logging functionality, we add additional custom logging extensions to populate our event telemetry objects, and then pass them into the custom pipeline built with the Application Insights Client SDK.
See either Program.cs in SampleApp.ConsoleAppCore or SampleApp.ConsoleApp project to create and use an ILogger.

## Application Insights pipeline customization
The sample implements a custom pipeline that extends the standard Applications Insights Client SDK.  The custom pipeline includes enrichment properties to the event object and adds the following processor sinks to support the following target services:
- Application Insights for Aggregated Metrics 
- Application Insights OpenSchema custom schemas (LogOpenSchema, ExceptionsOpenSchema, TimedOperationOpenSchema)
- Elasticsearch uses the custom schemas (LogOpenSchema, ExceptionsOpenSchema but not TimedOperationOpenSchema))
- Graphite for Aggregated Metrics

## Query and Analytics
To view the events written to the above target services use:
- Application Insights Analytics to view
	- Aggregated Metrics (customEvents schema which is one of the default schemas in Application Insights)
	- OpenSchema data sources (custom schemas created by you, 3 are provided in the sample: LogOpenSchema, ExceptionsOpenSchema, TimedOperationOpenSchema)
- Elasticsearch uses Kibana to view events
- Graphite uses Grafana to view events

## Configuration
An appsettings.json file is required for each application. Replace these values with yours in the sections for [ApplicationInsights](#applicationinsights) and [TelemetryProcessorSinks](#telemetryprocessorsinks).

### ApplicationInsights
```json
"ApplicationInsights": {
    "InstrumentationKey": "{{Secret:AIInstrumentationKey}}",
    "TelemetryServiceEndpoint": "https://dc.services.visualstudio.com/v2/track",

    "InMemoryPublishingChannel": {
      "maxBacklog": 1000,
      "maxWindowCount": 500,
      "windowSize": "00:00:15"
    },

    "LogLevel": {
      "Default": "Information",
      "SampleAppCore": "Information"
    },
```

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `InstrumentationKey` | "{{Secret:AIInstrumentationKey}}" | Yes | Application Insights application instrumentation key. |
| `TelemetryServiceEndpoint` | url | Yes | Default Application Insights endpoint. |
| `InMemoryPublishingChannel` | JSON | No | Task DataFlow buffer block max sizes and timers. |
| `LogLevel` | JSON | No | Category names and their logging level. |

*InMemoryPublishingChannel*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `maxBacklog` | Integer | No | Buffer size of events for Application Insights. |
| `maxWindowCount` | Integer | No | Batch size of events before events are processed. |
| `windowSize` | Integer | No | Timer window size in HH:MM:SS format before events are processed. |

*LogLevel*

One or more CategoryNames with their respective logging level.

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| *Category Name* | Trace,Debug,Information,Warning,Error,Critical,None | No | Default is the default category name with default of level Error. |
	
### TelemetryProcessorSinks

[AggMetrics](#aggmetrics), [Graphite](#graphite), [ElasticSearch](#elasticsearch), [OpenSchema](#openschema)

#### AggMetrics
```json
{
	"name": "AggMetrics",
	"classAssembly": "Microsoft.AzureCAT.AppInsights.AppInsightAggMetricSinkFactory, Microsoft.AzureCAT.AppInsights.Sink.AggregatedMetric, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
	"maxBacklog": 10000,
	"maxWindowCount": 1000,
	"windowSize": "00:00:15"
}
```

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `name` | "AggMetrics" | Yes | Aggregated Metrics from ILogger.LogMetrics extension. |
| `classAssembly` | Implementation | Yes | ITelemetryProcessorFactory returns ITelemetryProcessor. |
| `maxBacklog` | Integer | No | Buffer size of metric events to be aggregated. |
| `maxWindowCount` | Integer | No | Batch size of metric events to be aggregated before events are processed. |
| `windowSize` | Integer | No | Timer window size in HH:MM:SS format before events are processed. |

#### Graphite
```json
{
	"name": "graphite",
	"classAssembly": "Microsoft.AzureCAT.AppInsights.AppInsightGraphiteSinkFactory, Microsoft.AzureCAT.AppInsights.Sink.Graphite, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
	"server": "{{Secret:GraphiteServer}}",
	"port": 2003,
	"maxBacklog": 1000,
	"maxWindowCount": 1000,
	"windowSize": "00:00:05",
	"EventTelemetryProperties": [
	  "Environment",
	  "MachineRole",
	  "MachineName"	]
}
```

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `name` | "graphite" | Yes | Aggregated Metrics from ILogger.LogMetrics extension. |
| `server` | "{{Secret:GraphiteServer}}"" | Yes | Replace with server dns name, "mygraphite.eastus.cloudapp.azure.com". |
| `port` | Integer | No | Port to write metrics to Graphite (default 2003). |
| `maxBacklog` | Integer | No | Buffer size of metric events to be aggregated. |
| `maxWindowCount` | Integer | No | Batch size of metric events to be aggregated before events are processed. |
| `windowSize` | Integer | No | Timer window size in HH:MM:SS format before events are processed. |
| `EventTelemetryProperties` | JSON array | No | Prefix properties to build the Graphite metric name. |

	
#### Elasticsearch
```json
{
	"name": "elasticsearch",
	"classAssembly": "Microsoft.AzureCAT.AppInsights.AppInsightElasticSearchSinkFactory, Microsoft.AzureCAT.AppInsights.Sink.ElasticSearch, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
	"indexnamebase": "{{Secret:ElasticSearchIndexBase}}",
	"elasticsearchurl": "{{Secret:ElasticSearchUrl}}",
	"port": 9201,
	"username": "{{Secret:ElasticSearchUsername}}",
	"password": "{{Secret:ElasticSearchPassword}}",
	"maxBacklog": 1000,
	"maxWindowCount": 1000,
	"windowSize": "00:00:15",
}
```

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `name` | "elasticsearch" | Yes | Elasticsearch telemetry processor sink. |
| `classAssembly` | Implementation | Yes | ITelemetryProcessorFactory returns ITelemetryProcessor. |
| `indexnamebase` | "{{Secret:ElasticSearchIndexBase}}" | Yes | Elasticsearch indexbase, date will be appended to the name (_index). |
| `typename` | "{{Secret:ElasticSearchTypeName}}" | No | Elasticsearch type name (_type). |
| `elasticsearchurl` | "{{Secret:ElasticSearchUrl}}" | Yes | Elasticsearch server, "http://myelasticsearch.eastus.cloudapp.azure.com". |
| `port` | Integer | No | Port to batch load data into ElasticSearch (9202). |
| `username` | "{{Secret:ElasticSearchUsername}}" | No | Elasticsearch user name, if not provided user id and password won't be used. |
| `password` | "{{Secret:ElasticSearchPassword}}" | No | Elasticsearch password, default is none. |
| `maxBacklog` | Integer | No | Buffer size of events to be sent elasticsearch. |
| `maxWindowCount` | Integer | No | Batch size of events before events are processed. |
| `windowSize` | Integer | No | Timer window size in HH:MM:SS format before events are processed. |


#### OpenSchema
```json
{
	"name": "OpenSchema",
	"classAssembly": "Microsoft.AzureCAT.AppInsights.AppInsightBlobSinkFactory, Microsoft.AzureCAT.AppInsights.Sink.Blob, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
	"InstrumentationKey": "{{Secret:AIInstrumentationKey}}",

	"BlobPublisher": {
	  "StorageAccounts": [
		"DefaultEndpointsProtocol=https;AccountName={{Secret:BlobPublisherStorageAccountName1}};AccountKey={{Secret:BlobPublisherStorageAccountkey1}}"
	  ],
	  "BaseContainerName": "{{Secret:BaseContainerName}}"
	},

	"BatchBuffer": {
	  "maxBacklog": 10000,
	  "maxWindowCount": 1000,
	  "windowSize": "00:00:15"
	},

	"BlobSinkBuffer": {
	  "memoryBufferBytes": 20485760,
	  "bufferFlush": "00:00:05",
	  "useGzip": true
	},

	"SchemaIdList": {
	  "LogOpenSchema": {
		"Id": "{{Secret:LogOpenSchemaId}}",
		"TransformOutput": {
		  "name": "LogOpenSchema",
		  "classAssembly": "Sample.OpenSchemas.BlobSinkToOpenSchema, Sample.OpenSchemas"
		}
	  },
	  "TimedOperationOpenSchema": {
		"Id": "{{Secret:TimedOperationOpenSchemaId}}",
		"TransformOutput": {
		  "name": "TimedOperationOpenSchema",
		  "classAssembly": "Sample.OpenSchemas.BlobSinkToOpenSchema, Sample.OpenSchemas"
		}
	  },
	  "ExceptionsOpenSchema": {
		"Id": "{{Secret:ExceptionsOpenSchemaId}}",
		"TransformOutput": {
		  "name": "ExceptionsOpenSchema",
		  "classAssembly": "Sample.OpenSchemas.BlobSinkToOpenSchema, Sample.OpenSchemas"
		}
	  }
	}
}
```

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `name` | "OpenSchema" | Yes | Application Insights OpenSchema telemetry processor sink. |
| `classAssembly` | Implementation | Yes | ITelemetryProcessorFactory returns ITelemetryProcessor. |
| `InstrumentationKey` | "{{Secret:AIInstrumentationKey}}" | Yes | Application Insights application instrumentation key. |

*BlobPublisher*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `StorageAccounts` | "Array of Azure Storage Accounts" | Yes | Array of Azure Storage Accounts. |
| `BaseContainerName` | "{{Secret:BaseContainerName}}" | Yes | Blob container name, unique per hour. |

*BlobPublisher-StorageAccounts*

1 or more Azure Storage Accounts

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `AccountName` | "{{Secret:BlobPublisherStorageAccountName1}}" | Yes | Azure Storage Account Name. |
| `AccountKey` | {{Secret:BlobPublisherStorageAccountkey1}} | Yes | Azure Storage Account Key. |

*BatchBuffer*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `maxBacklog` | Integer | No | Buffer size of events to be written to memory buffer (BlobSinkBuffer). |
| `maxWindowCount` | Integer | No | Batch size of events written to memory buffer. |
| `windowSize` | Integer | No | Timer window size in HH:MM:SS format before events are processed. |

*BlobSinkBuffer*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `memoryBufferBytes` | Integer | No | Max memory buffer size in bytes of blob to be written and ingested (imported) into Application Insights OpenSchema. |
| `bufferFlush` | Integer | No | Timer window size in HH:MM:SS format before memory buffer is flushed. |
| `useGzip` | Boolean | No | Zip the blob, default is false. |

*SchemaIdList*

For each custom OpenSchema (LogOpenSchema, TimedOperationOpenSchema, ExceptionsOpenSchema), enter the GUID as the Id from the Azure portal for the Application Insights OpenSchema Other Data Sources:

*LogOpenSchema*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `Id` | "{{Secret:LogOpenSchemaId}}" | Yes | Schema Id GUID from the OpenSchema Other Data Sources. |
| `TransformOutput` | JSON | Yes | Transform (ITransformOutput) implementation to convert EventTelemetry to OpenSchema. |

*TimedOperationOpenSchema*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `Id` | "{{Secret:TimedOperationOpenSchemaId}}" | Yes | Schema Id GUID from the OpenSchema Other Data Sources. |
| `TransformOutput` | JSON | Yes | Transform (ITransformOutput) implementation to convert EventTelemetry to OpenSchema. |

*ExceptionsOpenSchema*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `Id` | "{{Secret:ExceptionsOpenSchemaId}}" | Yes | Schema Id GUID from the OpenSchema Other Data Sources. |
| `TransformOutput` | JSON | Yes | Transform (ITransformOutput) implementation to convert EventTelemetry to OpenSchema. |

*TransformOutput*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `name` | String | Yes | Schema class name matching LogTypes (LogOpenSchema, TimedOperationOpenSchema, ExceptionsOpenSchema). |
| `classAssembly` | Implementation | Yes | Transform (ITransformOutput) implementation to convert EventTelemetry to OpenSchema. |

## References
Using this samples assumes you are familiar with the general topics of monitoring, diagnostics, and analytics for Azure applications:
- [Application Insights](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-overview/)
- [Analytics in Application Insights](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-analytics/)
- [Application Insights Analytics is notified to import](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-analytics-import/)
- [Patterns and practices: Monitoring and diagnostics guidance](https://docs.microsoft.com/en-us/azure/best-practices-monitoring/)

## MSFT OSS Code Of Conduct Notice
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
