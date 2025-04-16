using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolProject.Models;

namespace SchoolProject.ViewModel
{
    public class UserManagementViewModel
    {
        public List<Account> Users { get; set; }
        public string SearchTerm { get; set; }
        public UserRole? RoleFilter { get; set; }
        public UserStatus? StatusFilter { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<SelectListItem> Roles { get; set; }
        public List<SelectListItem> Statuses { get; set; }
    }
}
