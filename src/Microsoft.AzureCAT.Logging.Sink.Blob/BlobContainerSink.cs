// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Configuration;
	using Microsoft.WindowsAzure.Storage;
	using Microsoft.WindowsAzure.Storage.Blob;
	using Microsoft.Extensions.Logging;

	/// <summary>
	/// Writes logs to storage account, choosing from a pool of storage accounts to use
	/// Uses new container name every hour, new blob filename for each writebuffer
	/// Only 1 memory buffer used
	/// GZip memory buffer before writing to blob
	/// </summary>
	public class BlobContainerSink<TInput, TOutput>
		: BatchingPublisherTransform<TInput, TOutput>, IProcessorSink<TInput>
	{
		// If the Event won't fit in our configurable memory buffer, log something to local logger
		private const int MaxDumpSizeBytes = 1000;

		private const int MaxBlobWriteAttempts = 10;

		const string GZIP_FILETYPE = ".gz";

		static string newLine = "\r\n";
		static byte[] newLineBytes = Encoding.UTF8.GetBytes(newLine);

		protected readonly string _openSchemaName;
		protected readonly string _fileType;

		protected BlobSinkBufferConfig _blobBufferConfig = new BlobSinkBufferConfig();

		private readonly SemaphoreSlim _semaphoreMemoryBuffer;
		private readonly MemoryStream _memoryBuffer;

		private readonly Func<CloudBlockBlob, Task> _blobWrittenFunc;
		private readonly Func<string> _containerNameFunc;
		private readonly List<CloudBlobClient> _blobClients;
		private Timer _timeMemoryBufferFlush;

		// Log number of events processed
		private int _eventCount = 0;
		private long _eventCountTotal = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="BlobContainerSink{TInput, TOutput}"/> class.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <param name="blobClients">The cloud blob clients to use for writing blobs (on differnt storage accounts).</param>
		/// <param name="onBlobWrittenFunc">The on BLOB written function.</param>
		/// <param name="containerNameFunc">The container name function (given a base name, returns the container name).</param>
		/// <param name="dataSource">The data source.</param>
		/// <param name="fileType">Type of the file.</param>
		public BlobContainerSink(
			IConfiguration config,
			ILogger logger,
			IEnumerable<CloudBlobClient> blobClients,
			Func<CloudBlockBlob, Task> onBlobWrittenFunc,
			Func<string> containerNameFunc,
			string dataSource,
			string fileType)
			: base(config: config.GetSection(BatchBufferConfig.BatchBufferConfigSection),
				logger: logger,
				name: dataSource)
		{
			_blobClients = new List<CloudBlobClient>(blobClients);
			_containerNameFunc = containerNameFunc;
			_blobWrittenFunc = onBlobWrittenFunc;
			_openSchemaName = dataSource;
			_fileType = fileType;

			// Semaphore for single memory buffer
			_semaphoreMemoryBuffer = new SemaphoreSlim(1, 1);

			// Configuration for MemoryBuffer and Write to Blob Timer
			var blobBufferConfig = config.GetSection(BlobSinkBufferConfig.BlobSinkBufferSection);
			blobBufferConfig.Bind(_blobBufferConfig);

			// Assign bytes arrary instead of byte count == fix size.
			var transmitBuffer = new byte[_blobBufferConfig.MemoryBufferBytes];
			_memoryBuffer = new MemoryStream(transmitBuffer);

			_timeMemoryBufferFlush = new Timer(FlushBuffer, null, _blobBufferConfig.BufferFlush, _blobBufferConfig.BufferFlush);

			// Change file extension for GZip
			if (_blobBufferConfig.UseGzip)
			{
				_fileType = _fileType + GZIP_FILETYPE;
			}
		}

		public virtual void ProcessEntry(TInput item)
		{
			base.PostEntry(item);
		}

		public override void Dispose()
		{
			// Anything in the buffer, write it!
			FlushWriteBufferToBlob();

			_semaphoreMemoryBuffer?.Wait();
			try
			{
				_memoryBuffer?.Dispose();
			}
			finally
			{
				_semaphoreMemoryBuffer?.Release();
			}

			_semaphoreMemoryBuffer?.Dispose();

			base.Dispose();
		}

		protected virtual void FlushBuffer(object state)
		{
			FlushWriteBufferToBlob();
		}

		protected virtual async Task WriteToBlobBuffer(byte[] byteEvent)
		{
			await _semaphoreMemoryBuffer.WaitAsync();
			try
			{
				// Is the Event bigger than the Memory Buffer?
				if (byteEvent.Length > _memoryBuffer.Capacity)
				{
					string snippet = Encoding.UTF8.GetString(
						byteEvent,
						0,
						(byteEvent.Length > MaxDumpSizeBytes ? MaxDumpSizeBytes : byteEvent.Length));
					_logger?.LogError(
						"{class}.{method} Event too large, dropping Event. Event={LargeEventSize} bytes, Memory Buffer={MemoryBufferSize} bytes {snippet}",
						nameof(BlobContainerSink<int, int>),
						nameof(WriteToBlobBuffer),
						byteEvent.Length,
						_memoryBuffer.Capacity,
						snippet);
					return;
				}

				if ((_memoryBuffer.Capacity - _memoryBuffer.Position) > (byteEvent.Length + newLineBytes.Length))
				{
					// NewLine for JSON or CSV format
					if (_memoryBuffer.Position != 0)
						_memoryBuffer.Write(newLineBytes, 0, newLineBytes.Length);

					_memoryBuffer.Write(byteEvent, 0, byteEvent.Length);
					_eventCount++;
					_eventCountTotal++;
				}
				else
				{
					// Flush the buffer and clear it (default) for re-use
					await WriteMemoryBuffer(_memoryBuffer);

					_memoryBuffer.Write(byteEvent, 0, byteEvent.Length);
					_eventCount++;
					_eventCountTotal++;
				}
			}
			finally
			{
				_semaphoreMemoryBuffer.Release();
			}
		}

		protected virtual async Task WriteMemoryBuffer(MemoryStream memoryStream, bool resetBuffer = true)
		{
			// Prepare memoryStream for Compression
			memoryStream.SetLength(memoryStream.Position);
			memoryStream.Position = 0;

			// Compress?
			if (_blobBufferConfig.UseGzip)
			{
				// Compress memory buffer
				using (var compressed = new MemoryStream())
				{
					using (GZipStream gz = new GZipStream(compressed, CompressionLevel.Fastest, true))
					{
						await memoryStream.CopyToAsync(gz);
					}
					compressed.Position = 0;
					await WriteBuffer(compressed, 0, compressed.Length).ConfigureAwait(false);
				}
			}
			else
			{
				// No Compression
				await WriteBuffer(memoryStream, 0, memoryStream.Length).ConfigureAwait(false);
			}

			// Reset Memory Buffer
			if (resetBuffer)
			{
				memoryStream.SetLength(0);
				memoryStream.Position = 0;
			}
		}

		protected virtual async Task WriteBuffer(Stream buffer, int offset, long length)
		{
			try
			{
				var blobPath = GetBlobName(_openSchemaName, _fileType);
				var container = GetContainerReference();
				var blobReference = container.GetBlockBlobReference(blobPath);

				// None of the stuff above might exist, rather than calling CreateIfNotExists every time, we will optimize for
				// perf and not call it unless we get an error
				int attempts = 0;
				while (true)
				{
					try
					{
						// Set buffer position before writing
						buffer.Position = offset;
						await blobReference.UploadFromStreamAsync(buffer, length).ConfigureAwait(false);
						await _blobWrittenFunc(blobReference).ConfigureAwait(false);
						// Reset Timer, otherwise we'll write too often resulting in some small files
						_timeMemoryBufferFlush.Change(_blobBufferConfig.BufferFlush, _blobBufferConfig.BufferFlush);

						_logger?.LogInformation(
							"{class}.{method} blob wrote {BlobSizeInBytes} bytes {GZip}, containing {EventCount} events, total {EventCountTotal}",
							nameof(BlobContainerSink<int, int>),
							nameof(WriteBuffer),
							length,
							_blobBufferConfig.UseGzip ? "UseGZip" : "NoGZip",
							_eventCount,
							_eventCountTotal);
						break;
					}
					catch (Exception e)
					{
						if (attempts++ > MaxBlobWriteAttempts)
						{
							_logger?.LogError(0, e, $"{nameof(BlobContainerSink<int, int>)}.{nameof(WriteBuffer)} Exception writing blob retries exhausted");
							// we are done retrying
							throw;
						}

						_logger?.LogError(0, e, $"{nameof(BlobContainerSink<int, int>)}.{nameof(WriteBuffer)} Exception writing blob, retrying");
						var tryAnotherAccount = true;
						var storageException = e as StorageException;
						if (storageException != null)
						{
							if (storageException.RequestInformation.HttpStatusCode == 404)
							{
								// not found so we need to create it
								try
								{
									var created = await container.CreateIfNotExistsAsync();
									_logger?.LogInformation(
										"Container not found, attempt to create resulted {ContainerCreationResult}",
										created);
									// we succeeeded to don't try a new account
									tryAnotherAccount = false;
								}
								catch (Exception ex)
								{
									_logger?.LogError(0, ex, "Exception creating container");
									tryAnotherAccount = true;
								}
							}
						}
						if (tryAnotherAccount)
						{
							// some other kind of error not related to not found so try another random container
							container = GetContainerReference();
							blobReference = container.GetBlockBlobReference(blobPath);
						}
					}
				}
			}
			finally
			{
				_eventCount = 0;
			}
		}

		protected virtual string GetBlobName(string dataSourceName, string fileTypeName)
		{
			// Use a Guid up front to improve load balancing
			return $"{Guid.NewGuid()}_{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}_{dataSourceName}.{fileTypeName}";
		}

		private void FlushWriteBufferToBlob()
		{
			try
			{
				_semaphoreMemoryBuffer.Wait();
				try
				{
					// If we didn't write buffer to the file but have something in the memory buffer that is not written yet, write it
					if (_memoryBuffer.Position > 0)
					{
						_logger?.LogInformation(
							"{class}.{method} {Position} bytes, containing {EventCount} events, total {EventCountTotal}",
							nameof(BlobContainerSink<int, int>),
							nameof(FlushWriteBufferToBlob),
							_memoryBuffer.Position,
							_eventCount,
							_eventCountTotal);

						// Flush the buffer and clear it (default) for re-use
						WriteMemoryBuffer(_memoryBuffer).Wait();
					}
				}
				finally
				{
					_semaphoreMemoryBuffer.Release();
				}
			}
			catch (Exception e)
			{
				_logger?.LogError(0, e, $"{nameof(BlobContainerSink<int, int>)}.{nameof(FlushWriteBufferToBlob)} exception flushing to blob storage");
			}
		}

		/// <summary>
		/// Gets the container reference (this may or may not exist at the time of calling)
		/// </summary>
		/// <returns></returns>
		private CloudBlobContainer GetContainerReference()
		{
			// Choose a random container from the list
			Random r = new Random();
			var account = _blobClients[r.Next(_blobClients.Count)];
			var containerName = _containerNameFunc();
			return account.GetContainerReference(containerName);
		}
	}
}