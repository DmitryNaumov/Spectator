using System.Collections.Generic;
using System.Diagnostics;

namespace Spectator
{
    internal interface IProcessManager
    {
        IReadOnlyCollection<Process> GetProcesses();
    }

    internal sealed class ProcessManager : IProcessManager
    {
        private IReadOnlyCollection<Process> _processes;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public IReadOnlyCollection<Process> GetProcesses()
        {
            if (_processes == null || _stopwatch.ElapsedMilliseconds > 10_000)
            {
                _processes = Process.GetProcesses();
                _stopwatch.Restart();
            }

            // TODO: filter exited or system

            return _processes;
        }
    }
}