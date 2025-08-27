using SchoolProject.Models;

namespace SchoolProject.ViewModel
{
    // ViewModels/DashboardViewModel.cs
    public class DashboardViewModel
    {
        public int ActiveModulesCount { get; set; }
        public int InactiveModulesCount { get; set; }
        public int TotalModules => ActiveModulesCount + InactiveModulesCount;
        public double ActivePercentage =>
        TotalModules == 0 ? 0 : (double)ActiveModulesCount / TotalModules * 100;

        public double InactivePercentage =>
            TotalModules == 0 ? 0 : (double)InactiveModulesCount / TotalModules * 100;


        public List<Account> RecentAccounts { get; set; } = new();
    }
}
