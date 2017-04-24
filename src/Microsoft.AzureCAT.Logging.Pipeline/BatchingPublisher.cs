// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCAT.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.Logging;

	public class BatchingPublisher<T> : IDisposable
	{
		protected BatchBufferConfig _batchBufferConfig = new BatchBufferConfig();
		protected readonly ILogger _logger;

		private readonly string _name;

		// TPL Dataflow pipeline objects and lifecycle management via CancellationToken
		protected readonly CancellationTokenSource _tokenSource;
		private BufferBlock<T> _buffer;
		private BatchBlock<T> _batcher;
		private ActionBlock<IEnumerable<T>> _publisher;
		private IDisposable[] _disposables;
		private int _disposeCount = 0;

		// Background timer to periodically flush the batch block
		private Timer _windowTimer;

		private long _droppedEvents;
		private long _droppedEventsTotal;

		public BatchingPublisher(
			IConfiguration config,
			ILogger logger,
			string name)
		{
			_tokenSource = new CancellationTokenSource();
			_logger = logger;
			_name = name;

			// Read BatchBufferConfig from config
			config.Bind(_batchBufferConfig);

			_buffer = new BufferBlock<T>(
				new ExecutionDataflowBlockOptions()
				{
					BoundedCapacity = _batchBufferConfig.MaxBacklog,
					CancellationToken = _tokenSource.Token
				});

			_batcher = new BatchBlock<T>(
				_batchBufferConfig.MaxWindowCount,
				new GroupingDataflowBlockOptions()
				{
					BoundedCapacity = _batchBufferConfig.MaxWindowCount,
					Greedy = true,
					CancellationToken = _tokenSource.Token
				});

			_publisher = new ActionBlock<IEnumerable<T>>(
				async (e) => await Publish(e),
				new ExecutionDataflowBlockOptions()
				{
					// Maximum of one concurrent batch being published
					MaxDegreeOfParallelism = 1,

					// Maximum of three pending batches to be published
					BoundedCapacity = 3,
					CancellationToken = _tokenSource.Token
				});

			_disposables = new IDisposable[]
			{
				_buffer.LinkTo(_batcher),
				_batcher.LinkTo(_publisher)
			};

			_windowTimer = new Timer(Flush, null, _batchBufferConfig.WindowSize, _batchBufferConfig.WindowSize);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{
			if (Interlocked.Increment(ref _disposeCount) == 1)
			{
				_windowTimer?.Dispose();

				_tokenSource.Cancel();
				_buffer.Completion.Wait();
				_batcher.Completion.Wait();
				_publisher.Completion.Wait();

				foreach (var d in _disposables)
					d.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public virtual void PostEntry(T item)
		{
			if (!Filter(item))
			{
				if (!_buffer.Post(item))
				{
					// Increase counter of dropped events
					Interlocked.Increment(ref _droppedEvents);
				}
			}
		}

		protected virtual bool Filter(T item)
		{
			return false;
		}

		protected virtual async Task Publish(IEnumerable<T> item)
		{
			await Task.FromResult(true);
		}

		/// <summary>
		/// Flushes the in-memory buffer and sends it.
		/// </summary>
		public void Flush(object state = null)
		{
			if (Interlocked.Read(ref _droppedEvents) != 0)
			{
				Interlocked.Add(ref _droppedEventsTotal, _droppedEvents);
				_logger?.LogWarning($"Dropped events {_name} Count {_droppedEvents} Total {_droppedEventsTotal}");
			}
			Interlocked.Exchange(ref _droppedEvents, 0);

			_batcher?.TriggerBatch();
		}
	}
}