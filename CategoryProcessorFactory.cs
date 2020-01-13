using System;
using System.Collections.Generic;
using System.Diagnostics;
using InfluxDB.LineProtocol.Payload;

namespace Spectator
{
    internal class CategoryProcessorFactory
    {
        public ICategoryProcessor GetCategoryProcessor(PerformanceCounterCategory category)
        {
            // TODO: resolve from container
            return new Win32CategoryProcessor(category);
        }
    }

    internal interface ICategoryProcessor
    {
        IReadOnlyCollection<ProcessData> Process(IReadOnlyCollection<Process> processes, InstanceDataCollectionCollection instanceDataCollection);
    }

    internal sealed class CounterData
    {
        public CounterData(string counterName, CounterSample sample)
        {
            CounterName = counterName;
            Sample = sample;
        }

        public string CounterName { get; }

        public CounterSample Sample { get; } // TODO: too heavy to store it for a while
    }

    internal sealed class ProcessData
    {
        public ProcessData(Process process, string categoryName, IReadOnlyCollection<CounterData> counters)
        {
            ProcessName = process.ProcessName;
            ProcessId = process.Id;

            CategoryName = categoryName;
            Counters = counters;
        }

        public ProcessData(string processName, int processId, string categoryName, IReadOnlyCollection<CounterData> counters)
        {
            ProcessName = processName;
            ProcessId = processId;

            CategoryName = categoryName;
            Counters = counters;
        }

        public string ProcessName { get; }

        public int ProcessId { get; }

        public string CategoryName { get; }
        public IReadOnlyCollection<CounterData> Counters { get; }

        // HACK:
        public string Host { get; set; }

        public DateTime Timestamp { get; set; }

        // HACK:
        public ProcessData Clone()
        {
            return new ProcessData(ProcessName, ProcessId + 1, CategoryName, Counters);
        }
    }
}