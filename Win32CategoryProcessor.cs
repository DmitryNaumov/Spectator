using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Spectator
{
    internal sealed class Win32CategoryProcessor : ICategoryProcessor
    {
        private readonly PerformanceCounterCategory _category;

        private readonly IReadOnlyDictionary<string, PerformanceCounter> _counters;

        public Win32CategoryProcessor(PerformanceCounterCategory category)
        {
            _category = category;

            var instanceNames = category.GetInstanceNames();
            var counters = instanceNames.Length == 0
                ? category.GetCounters()
                : category.GetCounters(instanceNames[0]);

            _counters = counters.ToDictionary(c => c.CounterName, c => c);
        }

        public IReadOnlyCollection<ProcessData> Process(IReadOnlyCollection<Process> processes, InstanceDataCollectionCollection instanceDataCollectionCollection)
        {
            var rawProcessData = new Dictionary<string, List<CounterData>>();

            var instanceNameMap = new Dictionary<string, Process>(processes.Count);

            var result = new List<ProcessData>();
            try
            {
                /*
                foreach (var process in processes)
                {
                }
                */

                foreach (string counterName in instanceDataCollectionCollection.Keys)
                {
                    var instanceDataCollection = instanceDataCollectionCollection[counterName];
                    foreach (string instanceName in instanceDataCollection.Keys)
                    {
                        var instanceData = instanceDataCollection[instanceName];

                        if (!rawProcessData.TryGetValue(instanceName, out var list))
                        {
                            var process = FindProcess(instanceName, processes);
                            if (process == null)
                                continue;

                            instanceNameMap.Add(instanceName, process);

                            list = new List<CounterData>(/* TODO: capacity */);
                            rawProcessData.Add(instanceName, list);
                        }

                        list.Add(new CounterData(counterName, instanceData.Sample));
                    }
                }

                result = rawProcessData.Select(pair =>
                    new ProcessData(instanceNameMap[pair.Key], _category.CategoryName, pair.Value)).ToList();
            }
            catch (Exception ex)
            {
            }

            return result;
        }

        private Process FindProcess(string instanceName, IReadOnlyCollection<Process> processes)
        {
            var processName = instanceName.Split('#')[0];

            return processes.FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}