using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Spectator
{
    internal interface IProfileManager
    {
        IReadOnlyCollection<PerformanceCounterCategory> GetCategories(IReadOnlyCollection<Process> processes);
    }

    internal sealed class ProfileManager : IProfileManager
    {
        private IReadOnlyCollection<PerformanceCounterCategory> _categories;

        public IReadOnlyCollection<PerformanceCounterCategory> GetCategories(IReadOnlyCollection<Process> processes)
        {
            if (_categories == null)
            {
                _categories = PerformanceCounterCategory
                    .GetCategories()
                    .Where(c => c.CategoryType == PerformanceCounterCategoryType.MultiInstance)
                    .ToList();
            }

            return _categories;
        }
    }
}