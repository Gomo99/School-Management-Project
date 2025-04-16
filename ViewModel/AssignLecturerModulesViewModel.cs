using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SchoolProject.ViewModel
{
    public class AssignLecturerModulesViewModel
    {
        public List<int> SelectedModuleIDs { get; set; } = new();
        public int UserID { get; set; }
        public DateTime AssignedDate { get; set; }
    }

}
