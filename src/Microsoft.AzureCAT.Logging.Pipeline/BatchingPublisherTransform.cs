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

	public class BatchingPublisherTransform<TInput, TOutput> : IDisposable
	{
		protected BatchBufferConfig _batchBufferConfig = new BatchBufferConfig();
		protected readonly ILogger _logger;

		private readonly string _name;

		// TPL Dataflow pipeline objects and lifecycle management via CancellationToken
		private readonly CancellationTokenSource _tokenSource;
		private BufferBlock<TInput> _buffer;
		private BatchBlock<TInput> _batcher;
		private TransformBlock<IEnumerable<TInput>, IEnumerable<TOutput>> _transform;
		private ActionBlock<IEnumerable<TOutput>> _publisher;
		private IDisposable[] _disposables;
		private int _disposeCount = 0;

		// Background timer to periodically flush the batch block
		private Timer _windowTimer;

		private long _droppedEvents;
		private long _droppedEventsTotal;

		public BatchingPublisherTransform(
			IConfiguration config,
			ILogger logger,
			string name)
		{
			_tokenSource = new CancellationTokenSource();
			_logger = logger;
			_name = name;

			// Read BatchBufferConfig from config
			config.Bind(_batchBufferConfig);

			_buffer = new BufferBlock<TInput>(
				new ExecutionDataflowBlockOptions()
				{
					BoundedCapacity = _batchBufferConfig.MaxBacklog,
					CancellationToken = _tokenSource.Token
				});

			_batcher = new BatchBlock<TInput>(
				_batchBufferConfig.MaxWindowCount,
				new GroupingDataflowBlockOptions()
				{
					BoundedCapacity = _batchBufferConfig.MaxWindowCount,
					Greedy = true,
					CancellationToken = _tokenSource.Token
				});

			_transform = new TransformBlock<IEnumerable<TInput>, IEnumerable<TOutput>>(
				transform: (e) => Transform(e),
				dataflowBlockOptions: new ExecutionDataflowBlockOptions()
				{
					CancellationToken = _tokenSource.Token
				});

			_publisher = new ActionBlock<IEnumerable<TOutput>>(
				async (e) => await Publish(e),
				new ExecutionDataflowBlockOptions()
				{
					MaxDegreeOfParallelism = 1,
					BoundedCapacity = 32,
					CancellationToken = _tokenSource.Token
				});

			_disposables = new IDisposable[]
			{
				_buffer.LinkTo(_batcher),
				_batcher.LinkTo(_transform),
				_transform.LinkTo(_publisher)
			};

			_windowTimer = new Timer(Flush, null, _batchBufferConfig.WindowSize, _batchBufferConfig.WindowSize);
		}

		public virtual void Dispose()
		{
			if (Interlocked.Increment(ref _disposeCount) == 1)
			{
				_windowTimer?.Dispose();

			_tokenSource.Cancel();
			_buffer.Completion.Wait();
			_batcher.Completion.Wait();
			_transform.Completion.Wait();
			_publisher.Completion.Wait();

			foreach (var d in _disposables)
				d.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public void PostEntry(TInput item)
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

		protected virtual bool Filter(TInput item)
		{
			return false;
		}

		protected virtual async Task Publish(IEnumerable<TOutput> items)
		{
			await Task.FromResult(true);
		}

		protected virtual IEnumerable<TOutput> Transform(IEnumerable<TInput> items)
		{
			return null;
		}

		private void Flush(object state = null)
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