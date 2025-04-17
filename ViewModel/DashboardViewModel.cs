using SchoolProject.Models;
using X.PagedList;

namespace SchoolProject.ViewModel
{
    // ViewModels/DashboardViewModel.cs
    public class DashboardViewModel
    {
        public int ActiveModulesCount { get; set; }
        public int InactiveModulesCount { get; set; }
        public int TotalModules => ActiveModulesCount + InactiveModulesCount;
    }
}
