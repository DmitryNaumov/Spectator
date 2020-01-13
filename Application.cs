using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;

namespace Spectator
{
    // TODO: as hosted service
    internal sealed class Application : IStartable, IDisposable
    {
        private readonly IProcessManager _processManager;
        private readonly IProfileManager _profileManager;
        private readonly CategoryProcessorFactory _categoryProcessorFactory;
        private readonly IProcessDataStorage _storage;

        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

        public Application(IProcessManager processManager, IProfileManager profileManager, CategoryProcessorFactory categoryProcessorFactory, IProcessDataStorage storage)
        {
            _processManager = processManager;
            _profileManager = profileManager;
            _categoryProcessorFactory = categoryProcessorFactory;
            _storage = storage;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                var stopwatch = new Stopwatch();

                while (!_disposeCts.IsCancellationRequested)
                {
                    stopwatch.Restart();

                    DoOnce();

                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds < 1000)
                    {
                        await Task.Delay(1000 - (int)stopwatch.ElapsedMilliseconds);
                    }
                }
            });
        }

        private void DoOnce()
        {
            var processes = _processManager.GetProcesses();

            var categories = _profileManager.GetCategories(processes);

            var processCategory = categories.First(c => c.CategoryName == "Process");

            // HACK:
            categories = new[] { processCategory };

            var categoryData = categories.ToDictionary(c => c, c => c.ReadCategory());

            foreach (var pair in categoryData)
            {
                var processor = _categoryProcessorFactory.GetCategoryProcessor(pair.Key);

                var processData = processor.Process(processes, pair.Value);

                // emulate number of machines
                foreach (var host in GetHosts())
                {
                    var timestamp = DateTime.UtcNow;

                    // HACK
                    foreach (var data in processData)
                    {
                        data.Host = host;
                        data.Timestamp = timestamp;
                    }

                    _storage.Store(processData); // TODO: await

                    processData = processData.Select(p => p.Clone()).ToList();
                }
            }
        }

        void IDisposable.Dispose()
        {
            _disposeCts.Cancel();
        }

        private IEnumerable<string> GetHosts()
        {
            yield return Environment.MachineName;

            int n = 1;
            while (n++ <= 1000)
            {
                yield return "vHost" + n;
            }
        }
    }
}