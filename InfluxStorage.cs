using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;

namespace Spectator
{
    internal interface IProcessDataStorage
    {
        Task Store(IReadOnlyCollection<ProcessData> processData);
    }

    internal sealed class InfluxStorage : IProcessDataStorage, IStartable, IDisposable
    {
        private readonly Channel<ProcessData> _buffer;
        private readonly LineProtocolClient _client;

        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

        public InfluxStorage()
        {
            var options = new BoundedChannelOptions(1_000_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            };

            _buffer = Channel.CreateBounded<ProcessData>(options);

            _client = new LineProtocolClient(new Uri("http://localhost:8086"), "LoadTest");
        }

        public Task Store(IReadOnlyCollection<ProcessData> processData)
        {
            foreach (var item in processData)
            {
                if (!_buffer.Writer.TryWrite(item))
                    throw new InvalidProgramException();
            }

            return Task.CompletedTask;
        }

        void IStartable.Start()
        {
            Task.Run(async () =>
            {
                const int batchSize = 5000;

                while (await _buffer.Reader.WaitToReadAsync(_disposeCts.Token))
                {
                    var batch = new List<ProcessData>(batchSize);

                    while (_buffer.Reader.TryRead(out var item))
                    {
                        batch.Add(item);
                        if (batch.Count >= batchSize)
                            break;
                    }

                    var payload = new LineProtocolPayload();
                    foreach (var item in batch)
                    {
                        var fields = item.Counters.ToDictionary(c => Canonize(c.CounterName), c => c.Sample.RawValue as object);
                        var tags = new Dictionary<string, string>
                        {
                            { "process_name", item.ProcessName },
                            { "pid", item.ProcessId.ToString() },
                            { "host", item.Host }
                        };

                        var point = new LineProtocolPoint(Canonize(item.CategoryName), fields, tags, item.Timestamp);
                        payload.Add(point);
                    }

                    await _client.WriteAsync(payload);
                }
            });
        }

        void IDisposable.Dispose()
        {
            _disposeCts.Cancel();
        }

        private static string Canonize(string value)
        {
            return value.Replace(' ', '_');
        }
    }
}